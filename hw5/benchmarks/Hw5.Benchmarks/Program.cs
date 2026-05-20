using BenchmarkDotNet.Running;

namespace Hw5.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(IndexQueryBenchmarks).Assembly).Run(args, StableBenchmarkConfig.Create());
        return 0;
    }
}
