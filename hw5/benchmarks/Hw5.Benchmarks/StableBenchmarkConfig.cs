using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace Hw5.Benchmarks;

public static class StableBenchmarkConfig
{
    public static IConfig Create()
    {
        var settings = BenchRuntime.Current;
        var art = Path.GetFullPath(settings.ArtifactDirectory);
        Directory.CreateDirectory(art);

        return ManualConfig
            .Create(DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator))
            .WithArtifactsPath(art)
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddJob(
                Job.Default
                    .WithLaunchCount(1)
                    .WithWarmupCount(settings.WarmupCount)
                    .WithIterationCount(settings.IterationCount)
                    .WithId("Warm"))
            .AddJob(
                Job.Default
                    .WithLaunchCount(1)
                    .WithWarmupCount(0)
                    .WithIterationCount(Math.Max(3, settings.IterationCount / 2))
                    .WithId("Cold"));
    }
}
