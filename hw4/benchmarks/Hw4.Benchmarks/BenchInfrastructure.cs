using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;
using System.Runtime.CompilerServices;

namespace Hw4.Benchmarks;

internal static class HashHelpers
{
    public static int OpsSlice(int ops, int threads) =>
        Math.Max(1, ops / Math.Max(1, threads));

    public static int Mix(int seed)
    {
        unchecked
        {
            var x = (uint)seed;
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return (int)x & int.MaxValue;
        }
    }

    public static int PositiveMod(int x, int m) => ((x % m) + m) % m;
}

public static class BenchConfigurator
{
    public static IConfig Config(BenchSettings s)
    {
        var art = Path.GetFullPath(s.ArtifactDirectory);
        Directory.CreateDirectory(art);

        var baseManual = ManualConfig.Create(DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator))
            .WithArtifactsPath(art);

        if (Environment.GetEnvironmentVariable("HW4_BENCH_SMOKE") is "1")
        {
            return baseManual.AddJob(
                Job.Dry.DontEnforcePowerPlan().WithToolchain(InProcessNoEmitToolchain.Instance));
        }

        var job = Job.Default
            .DontEnforcePowerPlan()
            .WithToolchain(InProcessNoEmitToolchain.Instance)
            .WithWarmupCount(Math.Max(1, s.WarmupCount))
            .WithIterationCount(Math.Max(5, s.IterationCount))
            .WithInvocationCount(1)
            .WithUnrollFactor(1);

        return baseManual.AddJob(job);
    }
}

internal static class BenchSink
{
#pragma warning disable CA2211
    public static int SinkField;
#pragma warning restore CA2211

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AddHash(int h) => SinkField ^= h;
}
