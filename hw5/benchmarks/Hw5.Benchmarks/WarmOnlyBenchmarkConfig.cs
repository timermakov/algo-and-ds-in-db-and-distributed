using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace Hw5.Benchmarks;

/// <summary>Warm job only — keeps scaling/operators/build under ~30 min total.</summary>
public sealed class WarmOnlyBenchmarkConfig : ManualConfig
{
    public WarmOnlyBenchmarkConfig()
    {
        var settings = BenchRuntime.Current;
        var art = Path.GetFullPath(settings.ArtifactDirectory);
        Directory.CreateDirectory(art);

        WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithArtifactsPath(art)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddJob(
                Job.Default
                    .WithLaunchCount(1)
                    .WithWarmupCount(settings.WarmupCount)
                    .WithIterationCount(settings.IterationCount)
                    .WithId("Warm"));
    }
}
