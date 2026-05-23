using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Ranking;
using Hw5.SearchIndex.Searching;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

[Config(typeof(StableBenchmarkConfig))]
public class IndexQueryBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private DiskSegmentIndex _disk = null!;
    private string _segmentPath = string.Empty;
    private string _andQuery = string.Empty;
    private string _nearAdjQuery = string.Empty;
    private string _rankQuery = string.Empty;
    private readonly QueryExecutor _executor = new();
    private readonly SearchService _search = new();

    [ParamsSource(nameof(SmokeCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> SmokeCases() => BenchCorpusCase.Smoke();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        (_disk, _segmentPath) = CorpusBenchmarkBuilder.BuildDiskIndex(_memory);
        var queries = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus);
        _andQuery = queries[0];
        _nearAdjQuery = queries.Length > 4 ? queries[4] : "alpha NEAR/3 beta OR gamma ADJ delta";
        _rankQuery = queries.Length > 1 ? queries[1] : "alpha OR beta";
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
            total += _executor.Execute(_memory, _andQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int DiskMmap_AndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_disk, _andQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int Memory_NearAdjQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _nearAdjQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public double Memory_Bm25Top10()
    {
        double sum = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            sum += _search.Search(_memory, _rankQuery, topK: 10, rankingMode: RankingMode.Bm25)
                .Sum(static x => x.Score);
        }

        return sum;
    }
}
