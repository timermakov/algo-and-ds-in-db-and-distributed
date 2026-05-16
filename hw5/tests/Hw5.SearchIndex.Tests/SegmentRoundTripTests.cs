using Hw5.SearchIndex.Compression;
using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Storage;

namespace Hw5.SearchIndex.Tests;

public sealed class SegmentRoundTripTests
{
    [Fact]
    public void CodecRoundTrip_WorksForDeltaEncodedData()
    {
        var original = new[] { 1, 2, 5, 9, 17, 33, 65 };
        var deltas = BitPackedDeltaCodec.DeltaEncode(original);
        var encoded = BitPackedDeltaCodec.Encode(deltas);
        var decoded = BitPackedDeltaCodec.Decode(encoded);
        var restored = BitPackedDeltaCodec.DeltaDecode(decoded);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void SegmentRoundTrip_QueriesMatchMemoryIndex()
    {
        var index = new InMemoryPositionalIndex();
        index.AddDocument(new SearchDocument(1, "red green blue green"));
        index.AddDocument(new SearchDocument(2, "blue yellow green"));
        index.AddDocument(new SearchDocument(3, "red yellow"));
        index.Seal();

        var path = Path.Combine(Path.GetTempPath(), $"hw5-segment-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, index);
            using var disk = new DiskSegmentIndex(path);

            var expected = index.GetPostings("green");
            var actual = disk.GetPostings("green");
            Assert.Equal(expected.Count, actual.Count);
            Assert.Equal(expected.DocumentIdAt(0), actual.DocumentIdAt(0));
            Assert.Equal(expected.PositionsAt(0), actual.PositionsAt(0));
            Assert.Equal(expected.DocumentIdAt(1), actual.DocumentIdAt(1));
            Assert.Equal(expected.PositionsAt(1), actual.PositionsAt(1));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void RandomizedCodecRoundTrip_IsStable()
    {
        var random = new Random(42);
        for (var t = 0; t < 30; t++)
        {
            var values = new List<int>();
            var cursor = 0;
            for (var i = 0; i < 200; i++)
            {
                cursor += random.Next(0, 5);
                values.Add(cursor);
            }

            var encoded = BitPackedDeltaCodec.Encode(BitPackedDeltaCodec.DeltaEncode(values));
            var decoded = BitPackedDeltaCodec.DeltaDecode(BitPackedDeltaCodec.Decode(encoded));
            Assert.Equal(values, decoded);
        }
    }
}
