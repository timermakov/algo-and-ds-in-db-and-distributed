using Hw5.SearchIndex.Corpus;
using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

internal static class CorpusBenchmarkBuilder
{
    private static readonly string[] SyntheticVocabulary =
    [
        "alpha", "beta", "gamma", "delta", "omega", "zeta", "kappa", "lambda", "sigma", "theta",
    ];

    public static InMemoryPositionalIndex BuildMemoryIndex(CorpusKind kind, int documentCount)
    {
        return kind switch
        {
            CorpusKind.Synthetic => BuildSynthetic(documentCount),
            CorpusKind.Wikipedia => WikipediaJsonlReader.BuildIndex(BenchRuntime.WikipediaJsonlPath, documentCount),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    public static (DiskSegmentIndex Disk, string Path) BuildDiskIndex(InMemoryPositionalIndex memory)
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-bench-{Guid.NewGuid():N}.seg");
        SegmentSerializer.Write(path, memory);
        return (new DiskSegmentIndex(path), path);
    }

    public static NaivePositionalIndexReader BuildNaiveReader(CorpusKind kind, int documentCount)
    {
        var memory = BuildMemoryIndex(kind, documentCount);
        var docs = memory.AllDocumentIds
            .Select(id => memory.TryGetDocument(id, out var doc)
                ? doc
                : new SearchDocument(id, string.Empty))
            .ToArray();
        return new NaivePositionalIndexReader(docs);
    }

    public static string[] LoadQueries(CorpusKind kind)
    {
        if (kind == CorpusKind.Wikipedia)
        {
            var queryFile = Path.Combine(BenchRuntime.Hw5Root, "data", "queries", "wiki-bench-queries.txt");
            if (File.Exists(queryFile))
            {
                return File.ReadAllLines(queryFile).Where(static l => !string.IsNullOrWhiteSpace(l)).ToArray();
            }
        }

        return
        [
            "alpha AND beta",
            "alpha OR gamma",
            "alpha AND NOT delta",
            "alpha ADJ beta",
            "alpha NEAR/3 gamma",
        ];
    }

    private static InMemoryPositionalIndex BuildSynthetic(int documentCount)
    {
        var settings = BenchRuntime.Current;
        var random = new Random(settings.SyntheticSeed);
        var index = new InMemoryPositionalIndex();
        for (var docId = 1; docId <= documentCount; docId++)
        {
            var terms = Enumerable.Range(0, settings.TermsPerDocument)
                .Select(_ => SyntheticVocabulary[random.Next(SyntheticVocabulary.Length)]);
            index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
        }

        index.Seal();
        return index;
    }
}
