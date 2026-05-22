using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class IndexQueryScalingBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private DiskSegmentIndex _disk = null!;
    private string _segmentPath = string.Empty;
    private string _query = string.Empty;
    private readonly QueryExecutor _executor = new();

    [ParamsSource(nameof(AllCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> AllCases() => BenchCorpusCase.All();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        (_disk, _segmentPath) = CorpusBenchmarkBuilder.BuildDiskIndex(_memory);
        _query = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus)[0];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _disk.Dispose();
        if (File.Exists(_segmentPath))
        {
            File.Delete(_segmentPath);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int Memory_AndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _query).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int DiskMmap_AndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_disk, _query).Matches.DocumentIds.Count;
        }

        return total;
    }
}
