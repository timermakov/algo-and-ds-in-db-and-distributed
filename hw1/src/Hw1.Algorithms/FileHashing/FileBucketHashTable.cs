using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace Hw1.Algorithms.FileHashing;

public sealed class FileBucketHashTable : IDisposable
{
    private const ulong Magic = 0x3148534148454C49UL; // ILEHASH1
    private const int HeaderSize = 64;
    private const int EntrySize = 24;
    private const byte EntryEmpty = 0;
    private const byte EntryUsed = 1;
    private const byte EntryTombstone = 2;

    private readonly FileStream _fileStream;
    private readonly MemoryMappedFile _mappedFile;
    private readonly MemoryMappedViewAccessor _view;
    private readonly int _bucketCount;
    private readonly int _slotsPerBucket;
    private readonly long _entriesOffset;
    private bool _disposed;

    private FileBucketHashTable(
        FileStream fileStream,
        MemoryMappedFile mappedFile,
        MemoryMappedViewAccessor view,
        int bucketCount,
        int slotsPerBucket)
    {
        _fileStream = fileStream;
        _mappedFile = mappedFile;
        _view = view;
        _bucketCount = bucketCount;
        _slotsPerBucket = slotsPerBucket;
        _entriesOffset = HeaderSize;
    }

    public int BucketCount => _bucketCount;
    public int SlotsPerBucket => _slotsPerBucket;

    public static FileBucketHashTable Open(string path, FileBucketHashOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var mode = options.CreateNew ? FileMode.Create : FileMode.Open;
        var fileStream = new FileStream(
            path,
            mode,
            FileAccess.ReadWrite,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.RandomAccess);

        try
        {
            if (options.CreateNew)
            {
                InitializeEmptyFile(fileStream, options);
            }

            var mappedFile = MemoryMappedFile.CreateFromFile(
                fileStream,
                mapName: null,
                capacity: fileStream.Length,
                access: MemoryMappedFileAccess.ReadWrite,
                inheritability: HandleInheritability.None,
                leaveOpen: true);

            var view = mappedFile.CreateViewAccessor(0, fileStream.Length, MemoryMappedFileAccess.ReadWrite);
            var (bucketCount, slotsPerBucket) = ReadHeader(view);

            if (options.CreateNew && (bucketCount != options.BucketCount || slotsPerBucket != options.SlotsPerBucket))
            {
                throw new InvalidDataException("Header does not match requested options.");
            }

            return new FileBucketHashTable(fileStream, mappedFile, view, bucketCount, slotsPerBucket);
        }
        catch
        {
            fileStream.Dispose();
            throw;
        }
    }

    public bool TryGet(ulong key, out long value)
    {
        ThrowIfDisposed();
        var bucketId = BucketId(key);
        var (found, slotIndex) = FindSlotForKey(bucketId, key);
        if (!found)
        {
            value = default;
            return false;
        }

        value = ReadEntryValue(bucketId, slotIndex);
        return true;
    }

    public void Insert(ulong key, long value)
    {
        ThrowIfDisposed();
        var bucketId = BucketId(key);
        var (found, foundAt, insertionAt) = ProbeBucket(bucketId, key);
        if (found)
        {
            throw new InvalidOperationException("Key already exists.");
        }

        if (insertionAt < 0)
        {
            throw new InvalidOperationException("Bucket is full.");
        }

        WriteEntry(bucketId, insertionAt, EntryUsed, key, value);
    }

    public void Update(ulong key, long value)
    {
        ThrowIfDisposed();
        var bucketId = BucketId(key);
        var (found, slotIndex) = FindSlotForKey(bucketId, key);
        if (!found)
        {
            throw new KeyNotFoundException("Key not found.");
        }

        WriteEntry(bucketId, slotIndex, EntryUsed, key, value);
    }

    public bool Delete(ulong key)
    {
        ThrowIfDisposed();
        var bucketId = BucketId(key);
        var (found, slotIndex) = FindSlotForKey(bucketId, key);
        if (!found)
        {
            return false;
        }

        WriteEntry(bucketId, slotIndex, EntryTombstone, 0UL, 0L);
        return true;
    }

    public void WarmupBuckets()
    {
        ThrowIfDisposed();
        for (var bucket = 0; bucket < _bucketCount; bucket++)
        {
            for (var slot = 0; slot < _slotsPerBucket; slot++)
            {
                _ = ReadEntryState(bucket, slot);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _view.Dispose();
        _mappedFile.Dispose();
        _fileStream.Dispose();
        _disposed = true;
    }

    private static void InitializeEmptyFile(FileStream stream, FileBucketHashOptions options)
    {
        var entriesSize = checked((long)options.BucketCount * options.SlotsPerBucket * EntrySize);
        var totalSize = HeaderSize + entriesSize;
        stream.SetLength(totalSize);

        using var mappedFile = MemoryMappedFile.CreateFromFile(
            stream,
            mapName: null,
            capacity: totalSize,
            access: MemoryMappedFileAccess.ReadWrite,
            inheritability: HandleInheritability.None,
            leaveOpen: true);

        using var view = mappedFile.CreateViewAccessor(0, totalSize, MemoryMappedFileAccess.ReadWrite);
        WriteHeader(view, options.BucketCount, options.SlotsPerBucket);
    }

    private static void WriteHeader(MemoryMappedViewAccessor view, int bucketCount, int slotsPerBucket)
    {
        view.Write(0, Magic);
        view.Write(8, 1); // version
        view.Write(12, bucketCount);
        view.Write(16, slotsPerBucket);
    }

    private static (int bucketCount, int slotsPerBucket) ReadHeader(MemoryMappedViewAccessor view)
    {
        var magic = view.ReadUInt64(0);
        if (magic != Magic)
        {
            throw new InvalidDataException("Unknown hash table file format.");
        }

        var version = view.ReadInt32(8);
        if (version != 1)
        {
            throw new InvalidDataException("Unsupported file format version.");
        }

        var bucketCount = view.ReadInt32(12);
        var slotsPerBucket = view.ReadInt32(16);
        if (bucketCount <= 0 || slotsPerBucket <= 0)
        {
            throw new InvalidDataException("Corrupted file header.");
        }

        return (bucketCount, slotsPerBucket);
    }

    private int BucketId(ulong key)
    {
        var hash = Mix64(key);
        return (int)(hash % (uint)_bucketCount);
    }

    private (bool found, int slotIndex) FindSlotForKey(int bucketId, ulong key)
    {
        var (found, foundAt, _) = ProbeBucket(bucketId, key);
        return (found, foundAt);
    }

    private (bool found, int foundAt, int insertionAt) ProbeBucket(int bucketId, ulong key)
    {
        var insertionAt = -1;
        for (var slot = 0; slot < _slotsPerBucket; slot++)
        {
            var state = ReadEntryState(bucketId, slot);
            if (state == EntryUsed)
            {
                if (ReadEntryKey(bucketId, slot) == key)
                {
                    return (true, slot, insertionAt);
                }

                continue;
            }

            if (state == EntryTombstone)
            {
                if (insertionAt < 0)
                {
                    insertionAt = slot;
                }

                continue;
            }

            if (insertionAt < 0)
            {
                insertionAt = slot;
            }

            return (false, -1, insertionAt);
        }

        return (false, -1, insertionAt);
    }

    private byte ReadEntryState(int bucketId, int slot)
    {
        return _view.ReadByte(EntryOffset(bucketId, slot));
    }

    private ulong ReadEntryKey(int bucketId, int slot)
    {
        return _view.ReadUInt64(EntryOffset(bucketId, slot) + 8);
    }

    private long ReadEntryValue(int bucketId, int slot)
    {
        return _view.ReadInt64(EntryOffset(bucketId, slot) + 16);
    }

    private void WriteEntry(int bucketId, int slot, byte state, ulong key, long value)
    {
        var offset = EntryOffset(bucketId, slot);
        _view.Write(offset, state);
        _view.Write(offset + 8, key);
        _view.Write(offset + 16, value);
    }

    private long EntryOffset(int bucketId, int slot)
    {
        return _entriesOffset + (((long)bucketId * _slotsPerBucket + slot) * EntrySize);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix64(ulong x)
    {
        x ^= x >> 33;
        x *= 0xff51afd7ed558ccdUL;
        x ^= x >> 33;
        x *= 0xc4ceb9fe1a85ec53UL;
        x ^= x >> 33;
        return x;
    }
}
