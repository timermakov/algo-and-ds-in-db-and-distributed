using System.Text;
using Hw5.SearchIndex.Compression;
using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Storage;

public static class SegmentSerializer
{
    private const int FormatVersion = 1;
    private static readonly byte[] Magic = "HW5S"u8.ToArray();

    public static void Write(string path, IPositionalIndexReader index)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(index);

        var termEntries = BuildEntries(index);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false);
        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(index.DocumentCount);
        writer.Write(index.Terms.Count);

        foreach (var docId in index.AllDocumentIds.Order())
        {
            writer.Write(docId);
            writer.Write(index.GetDocumentLength(docId));
        }

        foreach (var entry in termEntries)
        {
            var termBytes = Encoding.UTF8.GetBytes(entry.Term);
            writer.Write(termBytes.Length);
            writer.Write(termBytes);
            writer.Write(entry.Segment.Offset);
            writer.Write(entry.Segment.Length);
            writer.Write(entry.Segment.DocumentFrequency);
        }

        foreach (var entry in termEntries)
        {
            writer.Write(entry.Payload);
        }
    }

    private static List<TermBuffer> BuildEntries(IPositionalIndexReader index)
    {
        var buffers = new List<TermBuffer>();
        foreach (var term in index.Terms.Order())
        {
            var postings = index.GetPostings(term);
            var payload = BuildPayload(postings);
            buffers.Add(new TermBuffer(term, payload, postings.Count));
        }

        var baseOffset = Magic.Length + sizeof(int) + sizeof(int) + sizeof(int);
        baseOffset += index.DocumentCount * sizeof(int) * 2;
        foreach (var buffer in buffers)
        {
            baseOffset += Encoding.UTF8.GetByteCount(buffer.Term) + sizeof(int);
            baseOffset += sizeof(long) + sizeof(int) + sizeof(int);
        }

        long offset = baseOffset;
        for (var i = 0; i < buffers.Count; i++)
        {
            var buffer = buffers[i];
            buffer = buffer with { Segment = new TermSegmentEntry(offset, buffer.Payload.Length, buffer.DocumentFrequency) };
            buffers[i] = buffer;
            offset += buffer.Payload.Length;
        }

        return buffers;
    }

    private static byte[] BuildPayload(PostingList postings)
    {
        var docIds = new int[postings.Count];
        var positionCounts = new int[postings.Count];
        var allPositions = new List<int>();
        for (var i = 0; i < postings.Count; i++)
        {
            docIds[i] = postings.DocumentIdAt(i);
            var positions = postings.PositionsAt(i);
            positionCounts[i] = positions.Count;
            allPositions.AddRange(BitPackedDeltaCodec.DeltaEncode(positions));
        }

        var docDeltas = BitPackedDeltaCodec.DeltaEncode(docIds);
        var encodedDocDeltas = BitPackedDeltaCodec.Encode(docDeltas);
        var encodedCounts = BitPackedDeltaCodec.Encode(positionCounts);
        var encodedPositions = BitPackedDeltaCodec.Encode(allPositions);

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(encodedDocDeltas.Length);
        writer.Write(encodedDocDeltas);
        writer.Write(encodedCounts.Length);
        writer.Write(encodedCounts);
        writer.Write(encodedPositions.Length);
        writer.Write(encodedPositions);
        writer.Flush();
        return stream.ToArray();
    }

    private sealed record TermBuffer(string Term, byte[] Payload, int DocumentFrequency)
    {
        public TermSegmentEntry Segment { get; init; } = new(0, 0, DocumentFrequency);
    }
}
