using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

internal static class BenchmarkIndexFactory
{
    private static readonly string[] Vocabulary =
    [
        "alpha", "beta", "gamma", "delta", "omega", "zeta", "kappa", "lambda", "sigma", "theta",
    ];

    public static InMemoryPositionalIndex BuildMemoryIndex()
    {
        var settings = BenchRuntime.Current;
        var random = new Random(42);
        var index = new InMemoryPositionalIndex();
        for (var docId = 1; docId <= settings.DocumentCount; docId++)
        {
            var terms = Enumerable.Range(0, settings.TermsPerDocument)
                .Select(_ => Vocabulary[random.Next(Vocabulary.Length)]);
            index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
        }

        index.Seal();
        return index;
    }

    public static (DiskSegmentIndex Disk, string Path) BuildDiskIndex(InMemoryPositionalIndex memory)
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-bench-{Guid.NewGuid():N}.seg");
        SegmentSerializer.Write(path, memory);
        return (new DiskSegmentIndex(path), path);
    }
}
