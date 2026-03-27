using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;

namespace Hw2.Benchmarks;

public sealed class StableBenchmarkConfig : ManualConfig
{
    public StableBenchmarkConfig()
    {
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(
            Job.Default
                .WithLaunchCount(1)
                .WithWarmupCount(15)
                .WithIterationCount(40)
                .WithId("Stable"));
    }
}
