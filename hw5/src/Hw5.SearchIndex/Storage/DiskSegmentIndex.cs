using System.IO.MemoryMappedFiles;
using System.Text;
using Hw5.SearchIndex.Compression;
using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Storage;

public sealed class DiskSegmentIndex : IPositionalIndexReader, IDisposable
{
    private static readonly byte[] Magic = "HW5S"u8.ToArray();
    private readonly string _path;
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly PagedMmapReader _pagedReader;
    private readonly Dictionary<string, TermSegmentEntry> _termDirectory = new(StringComparer.Ordinal);
    private readonly Dictionary<int, int> _documentLengths = new();
    private bool _disposed;

    public DiskSegmentIndex(string path)
    {
        _path = path;
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        ValidateHeader(reader);
        DocumentCount = reader.ReadInt32();
        var termCount = reader.ReadInt32();

        for (var i = 0; i < DocumentCount; i++)
        {
            var docId = reader.ReadInt32();
            var length = reader.ReadInt32();
            _documentLengths[docId] = length;
        }

        for (var i = 0; i < termCount; i++)
        {
            var termLength = reader.ReadInt32();
            var term = Encoding.UTF8.GetString(reader.ReadBytes(termLength));
            var offset = reader.ReadInt64();
            var length = reader.ReadInt32();
            var df = reader.ReadInt32();
            _termDirectory[term] = new TermSegmentEntry(offset, length, df);
        }

        _mmf = MemoryMappedFile.CreateFromFile(_path, FileMode.Open, null, 0L, MemoryMappedFileAccess.Read);
        _accessor = _mmf.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read);
        _pagedReader = new PagedMmapReader(_accessor);
    }

    public int DocumentCount { get; }

    public IReadOnlyCollection<int> AllDocumentIds => _documentLengths.Keys;

    public IReadOnlyCollection<string> Terms => _termDirectory.Keys;

    public int GetDocumentLength(int documentId)
    {
        return _documentLengths.TryGetValue(documentId, out var value) ? value : 0;
    }

    public PostingList GetPostings(string term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        var normalized = term.ToLowerInvariant();
        if (!_termDirectory.TryGetValue(normalized, out var entry))
        {
            return new PostingList([]);
        }

        var payload = _pagedReader.ReadRange(entry.Offset, entry.Length);
        using var stream = new MemoryStream(payload);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);

        var docDataLength = reader.ReadInt32();
        var docDeltas = BitPackedDeltaCodec.Decode(reader.ReadBytes(docDataLength));
        var docIds = BitPackedDeltaCodec.DeltaDecode(docDeltas);

        var countsDataLength = reader.ReadInt32();
        var counts = BitPackedDeltaCodec.Decode(reader.ReadBytes(countsDataLength));

        var posDataLength = reader.ReadInt32();
        var allPositionDeltas = BitPackedDeltaCodec.Decode(reader.ReadBytes(posDataLength));

        var postings = new List<Posting>(docIds.Length);
        var offset = 0;
        for (var i = 0; i < docIds.Length; i++)
        {
            var count = counts[i];
            var deltas = allPositionDeltas.AsSpan(offset, count).ToArray();
            offset += count;
            var positions = BitPackedDeltaCodec.DeltaDecode(deltas);
            postings.Add(new Posting(docIds[i], positions));
        }

        return new PostingList(postings);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _accessor.Dispose();
        _mmf.Dispose();
    }

    private static void ValidateHeader(BinaryReader reader)
    {
        var magic = reader.ReadBytes(Magic.Length);
        if (!magic.SequenceEqual(Magic))
        {
            throw new InvalidDataException("Unknown segment format.");
        }

        var version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported segment version {version}.");
        }
    }
}
