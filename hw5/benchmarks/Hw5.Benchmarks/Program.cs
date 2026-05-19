using BenchmarkDotNet.Running;

namespace Hw5.Benchmarks;

internal static class Program
{
    private static int Main(string[] args)
    {
        var config = new StableBenchmarkConfig();
        BenchmarkSwitcher.FromAssembly(typeof(IndexQueryBenchmarks).Assembly).Run(args, config);
        return 0;
    }
}
