using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Storage;

namespace Hw5.SearchIndex.Tests;

public sealed class RandomizedIntegrationTests
{
    [Fact]
    public void RandomizedMemoryAndDiskResultsMatch()
    {
        var random = new Random(123);
        var vocabulary = new[] { "alpha", "beta", "gamma", "delta", "omega", "zeta" };
        var index = new InMemoryPositionalIndex();
        for (var docId = 1; docId <= 60; docId++)
        {
            var length = random.Next(5, 14);
            var terms = Enumerable.Range(0, length).Select(_ => vocabulary[random.Next(vocabulary.Length)]);
            index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
        }

        index.Seal();
        var path = Path.Combine(Path.GetTempPath(), $"hw5-random-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, index);
            using var disk = new DiskSegmentIndex(path);
            var executor = new QueryExecutor();

            for (var i = 0; i < 50; i++)
            {
                var left = vocabulary[random.Next(vocabulary.Length)];
                var right = vocabulary[random.Next(vocabulary.Length)];
                var query = (i % 4) switch
                {
                    0 => $"{left} AND {right}",
                    1 => $"{left} OR NOT {right}",
                    2 => $"{left} NEAR/2 {right}",
                    _ => $"{left} ADJ {right}",
                };

                var memoryResult = executor.Execute(index, query).Matches.SortedDocumentIds();
                var diskResult = executor.Execute(disk, query).Matches.SortedDocumentIds();
                Assert.Equal(memoryResult, diskResult);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
