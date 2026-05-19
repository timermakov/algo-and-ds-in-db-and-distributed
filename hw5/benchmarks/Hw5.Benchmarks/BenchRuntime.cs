using System.Text.Json;

namespace Hw5.Benchmarks;

public sealed class BenchSettings
{
    public int DocumentCount { get; init; } = 2000;
    public int TermsPerDocument { get; init; } = 24;
    public int WarmupCount { get; init; } = 3;
    public int IterationCount { get; init; } = 8;
    public string ArtifactDirectory { get; init; } = "BenchmarkDotNet.Artifacts/hw5";
}

public static class BenchRuntime
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    static BenchRuntime()
    {
        var paths = new[]
        {
            Environment.GetEnvironmentVariable("HW5_BENCH_CONFIG"),
            Path.Combine(AppContext.BaseDirectory, "bench.settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "bench.settings.json"),
        };

        var loaded = new BenchSettings();
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

                loaded = JsonSerializer.Deserialize<BenchSettings>(File.ReadAllText(full), JsonOpts) ?? new BenchSettings();
                break;
            }
            catch
            {
                /* optional settings */
            }
        }

        Current = ApplySmokeIfNeeded(loaded);
    }

    public static BenchSettings Current { get; }

    private static BenchSettings ApplySmokeIfNeeded(BenchSettings baseline) =>
        string.Equals(Environment.GetEnvironmentVariable("HW5_BENCH_SMOKE"), "1", StringComparison.Ordinal)
            ? new BenchSettings
            {
                DocumentCount = 128,
                TermsPerDocument = 12,
                WarmupCount = 0,
                IterationCount = 1,
                ArtifactDirectory = baseline.ArtifactDirectory,
            }
            : baseline;
}
