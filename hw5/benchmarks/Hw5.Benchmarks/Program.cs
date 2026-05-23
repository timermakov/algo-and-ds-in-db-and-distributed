using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace Hw5.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        var settings = BenchRuntime.Current;
        var art = Path.GetFullPath(settings.ArtifactDirectory);
        Directory.CreateDirectory(art);

        var baseline = ManualConfig
            .Create(DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator))
            .WithArtifactsPath(art)
            .AddDiagnoser(MemoryDiagnoser.Default);

        BenchmarkSwitcher.FromAssembly(typeof(IndexQueryBenchmarks).Assembly).Run(args, baseline);
        return 0;
    }
}
