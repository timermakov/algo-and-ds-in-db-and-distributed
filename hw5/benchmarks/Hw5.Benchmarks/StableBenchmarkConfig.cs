using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace Hw5.Benchmarks;

public sealed class StableBenchmarkConfig : ManualConfig
{
    public StableBenchmarkConfig()
    {
        var settings = BenchRuntime.Current;
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(
            Job.Default
                .WithLaunchCount(1)
                .WithWarmupCount(settings.WarmupCount)
                .WithIterationCount(settings.IterationCount)
                .WithId("Warm"));

        AddJob(
            Job.Default
                .WithLaunchCount(1)
                .WithWarmupCount(0)
                .WithIterationCount(Math.Max(3, settings.IterationCount / 2))
                .WithId("Cold"));
    }
}
