using Hw5.SearchIndex.Corpus;

namespace Hw5.Benchmarks;

public sealed class BenchCorpusCase
{
    public CorpusKind Corpus { get; set; }

    public int DocumentCount { get; set; }

    public override string ToString() => $"{Corpus}_{DocumentCount}";

    public static IEnumerable<BenchCorpusCase> All()
    {
        foreach (var row in BenchRuntime.CorpusDocumentCases())
        {
            yield return new BenchCorpusCase
            {
                Corpus = (CorpusKind)row[0]!,
                DocumentCount = (int)row[1]!,
            };
        }
    }

    public static IEnumerable<BenchCorpusCase> SmallOnly()
    {
        foreach (var n in new[] { 128, 512 })
        {
            yield return new BenchCorpusCase { Corpus = CorpusKind.Synthetic, DocumentCount = n };
        }
    }

    public static IEnumerable<BenchCorpusCase> MainCases()
    {
        var settings = BenchRuntime.Current;
        yield return new BenchCorpusCase
        {
            Corpus = CorpusKind.Synthetic,
            DocumentCount = settings.DocumentCounts[0],
        };

        if (!BenchRuntime.WikipediaAvailable || settings.WikiDocumentLimits.Length == 0)
        {
            yield break;
        }

        foreach (var n in settings.WikiDocumentLimits)
        {
            yield return new BenchCorpusCase
            {
                Corpus = CorpusKind.Wikipedia,
                DocumentCount = n,
            };
        }
    }

    public static IEnumerable<BenchCorpusCase> WikiMainCases()
    {
        if (!BenchRuntime.WikipediaAvailable || BenchRuntime.Current.WikiDocumentLimits.Length == 0)
        {
            yield break;
        }

        foreach (var n in BenchRuntime.Current.WikiDocumentLimits)
        {
            yield return new BenchCorpusCase { Corpus = CorpusKind.Wikipedia, DocumentCount = n };
        }
    }

    private static int WikiSmokeDocumentCount()
    {
        var limits = BenchRuntime.Current.WikiDocumentLimits;
        if (limits.Length == 0)
        {
            return 0;
        }

        var synthN = BenchRuntime.Current.DocumentCounts[0];
        return limits.Contains(synthN) ? synthN : limits[0];
    }

    public static IEnumerable<BenchCorpusCase> SyntheticOnly() =>
        All().Where(static c => c.Corpus == CorpusKind.Synthetic);

    public static IEnumerable<BenchCorpusCase> Smoke()
    {
        yield return new BenchCorpusCase { Corpus = CorpusKind.Synthetic, DocumentCount = BenchRuntime.Current.DocumentCounts[0] };
        if (BenchRuntime.WikipediaAvailable && BenchRuntime.Current.WikiDocumentLimits.Length > 0)
        {
            yield return new BenchCorpusCase
            {
                Corpus = CorpusKind.Wikipedia,
                DocumentCount = WikiSmokeDocumentCount(),
            };
        }
    }
}
