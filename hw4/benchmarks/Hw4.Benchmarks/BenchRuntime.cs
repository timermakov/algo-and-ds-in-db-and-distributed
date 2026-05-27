using System.Text.Json;

namespace Hw4.Benchmarks;

public sealed class BenchSettings
{
    public int[] ThreadCounts { get; init; } = [1, 2, 4, 8];
    public int KeySpace { get; init; } = 4096;
    public int OpsPerInvocation { get; init; } = 4096;
    public int WarmupCount { get; init; } = 3;
    public int IterationCount { get; init; } = 12;
    public string ArtifactDirectory { get; init; } = "BenchmarkDotNet.Artifacts";
}

public static class BenchRuntime
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static BenchRuntime()
    {
        var envPath = Environment.GetEnvironmentVariable("HW4_BENCH_CONFIG");
        var asmDir = Path.GetDirectoryName(typeof(BenchRuntime).Assembly.Location)!;
        var paths = new List<string>();

        if (!string.IsNullOrWhiteSpace(envPath))
            paths.Add(envPath);

        paths.Add(Path.Combine(asmDir, "bench.settings.json"));
        paths.Add(Path.Combine(asmDir, "..", "..", "..", "..", "config", "bench.settings.json"));
        paths.Add(Path.Combine(Directory.GetCurrentDirectory(), "config", "bench.settings.json"));

        var loaded = new BenchSettings();

        foreach (var raw in paths)
        {
            try
            {
                var full = Path.GetFullPath(raw);
                if (!File.Exists(full)) continue;

                var json = File.ReadAllText(full);
                loaded = JsonSerializer.Deserialize<BenchSettings>(json, JsonOpts) ?? new BenchSettings();
                Current = ApplySmokeIfNeeded(loaded);
                return;
            }
            catch
            {
                /* optional settings file */
            }
        }

        Current = ApplySmokeIfNeeded(loaded);
    }

    public static BenchSettings Current { get; }

    public static IEnumerable<int> ThreadCountsForParams() => Current.ThreadCounts;

    /// <summary><c>HW4_BENCH_SMOKE=1</c> — один прогон Dry (только проверка, что BDN стартует).</summary>
    static BenchSettings ApplySmokeIfNeeded(BenchSettings baseline) =>
        string.Equals(Environment.GetEnvironmentVariable("HW4_BENCH_SMOKE"), "1", StringComparison.Ordinal)
            ? new BenchSettings
            {
                ThreadCounts = [1],
                KeySpace = 64,
                OpsPerInvocation = 64,
                WarmupCount = 0,
                IterationCount = 1,
                ArtifactDirectory = baseline.ArtifactDirectory,
            }
            : baseline;
}
