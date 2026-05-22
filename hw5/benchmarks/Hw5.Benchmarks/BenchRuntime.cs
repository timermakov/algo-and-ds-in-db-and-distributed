using System.Text.Json;
using Hw5.SearchIndex.Corpus;

namespace Hw5.Benchmarks;

public sealed class SyntheticProfile
{
    public int Seed { get; init; } = 42;
    public int TermsPerDocument { get; init; } = 24;
}

public sealed class WikipediaProfile
{
    public string ManifestPath { get; init; } = "data/dataset.manifest.json";
    public string ProcessedPath { get; init; } = "data/processed/docs.jsonl";
    public int[] Limits { get; init; } = [5000, 20000];
}

public sealed class CorpusProfiles
{
    public SyntheticProfile Synthetic { get; init; } = new();
    public WikipediaProfile Wikipedia { get; init; } = new();
}

public sealed class BenchSettings
{
    public int[] DocumentCounts { get; init; } = [2000, 10000];
    public int[] WikiDocumentLimits { get; init; } = [5000, 20000];
    public int TermsPerDocument { get; init; } = 24;
    public int SyntheticSeed { get; init; } = 42;
    public int WarmupCount { get; init; } = 3;
    public int IterationCount { get; init; } = 8;
    public int ColdIterationCount { get; init; } = 8;
    public int OperationsPerInvoke { get; init; } = 32;
    public string ArtifactDirectory { get; init; } = "BenchmarkDotNet.Artifacts/hw5";
    public string WikipediaProcessedPath { get; init; } = "data/processed/docs.jsonl";
    public CorpusProfiles CorpusProfiles { get; init; } = new();

    [Obsolete("Use DocumentCounts[0]")]
    public int DocumentCount => DocumentCounts[0];
}

public static class BenchRuntime
{
    public const int DefaultOperationsPerInvoke = 32;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    static BenchRuntime()
    {
        Hw5Root = FindHw5Root();
        var loaded = LoadSettings();
        WikipediaJsonlPath = ResolveWikiPath(loaded.WikipediaProcessedPath);
        Current = ApplySmokeIfNeeded(loaded);
    }

    public static string Hw5Root { get; }

    public static BenchSettings Current { get; }

    public static string WikipediaJsonlPath { get; }

    public static bool WikipediaAvailable => WikipediaJsonlReader.IsAvailable(WikipediaJsonlPath);

    public static IEnumerable<object[]> CorpusDocumentCases()
    {
        foreach (var n in Current.DocumentCounts)
        {
            yield return [CorpusKind.Synthetic, n];
        }

        if (!WikipediaAvailable)
        {
            yield break;
        }

        foreach (var n in Current.WikiDocumentLimits)
        {
            yield return [CorpusKind.Wikipedia, n];
        }
    }

    public static string ResolveWikiPath(string relative)
    {
        if (Path.IsPathRooted(relative) && File.Exists(relative))
        {
            return relative;
        }

        foreach (var candidate in new[]
                 {
                     Path.Combine(Hw5Root, relative),
                     Path.Combine(AppContext.BaseDirectory, relative),
                     Path.Combine(Directory.GetCurrentDirectory(), relative),
                 })
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(Hw5Root, relative);
    }

    private static BenchSettings LoadSettings()
    {
        var paths = new[]
        {
            Environment.GetEnvironmentVariable("HW5_BENCH_CONFIG"),
            Path.Combine(AppContext.BaseDirectory, "bench.local.json"),
            Path.Combine(AppContext.BaseDirectory, "bench.settings.json"),
            Path.Combine(Hw5Root, "benchmarks", "Hw5.Benchmarks", "bench.local.json"),
            Path.Combine(Hw5Root, "benchmarks", "Hw5.Benchmarks", "bench.settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "bench.settings.json"),
        };

        foreach (var raw in paths)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            try
            {
                var full = Path.GetFullPath(raw);
                if (!File.Exists(full))
                {
                    continue;
                }

                return JsonSerializer.Deserialize<BenchSettings>(File.ReadAllText(full), JsonOpts) ?? new BenchSettings();
            }
            catch
            {
                /* optional settings */
            }
        }

        return new BenchSettings();
    }

    private static string FindHw5Root()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Hw5.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

    private static BenchSettings ApplySmokeIfNeeded(BenchSettings baseline)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("HW5_BENCH_SMOKE"), "1", StringComparison.Ordinal))
        {
            return baseline;
        }

        var wikiLimits = WikipediaAvailable ? new[] { Math.Min(128, baseline.WikiDocumentLimits.FirstOrDefault(5000)) } : Array.Empty<int>();
        return new BenchSettings
        {
            DocumentCounts = [128],
            WikiDocumentLimits = wikiLimits,
            TermsPerDocument = 12,
            WarmupCount = 0,
            IterationCount = 1,
            ColdIterationCount = 1,
            OperationsPerInvoke = 8,
            ArtifactDirectory = baseline.ArtifactDirectory,
            WikipediaProcessedPath = baseline.WikipediaProcessedPath,
            CorpusProfiles = baseline.CorpusProfiles,
        };
    }
}
