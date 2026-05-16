using System.IO.MemoryMappedFiles;

namespace Hw5.SearchIndex.Storage;

public sealed class PagedMmapReader
{
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly int _pageSize;

    public PagedMmapReader(MemoryMappedViewAccessor accessor, int pageSize = 4096)
    {
        _accessor = accessor;
        _pageSize = pageSize;
    }

    public byte[] ReadRange(long offset, int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var buffer = new byte[length];
        var copied = 0;
        while (copied < length)
        {
            var absoluteOffset = offset + copied;
            var offsetInPage = (int)(absoluteOffset % _pageSize);
            var available = _pageSize - offsetInPage;
            var toRead = Math.Min(available, length - copied);
            _accessor.ReadArray(absoluteOffset, buffer, copied, toRead);
            copied += toRead;
        }

        return buffer;
    }
}
