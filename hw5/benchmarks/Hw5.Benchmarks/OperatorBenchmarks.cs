using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class OperatorBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private string _andQuery = string.Empty;
    private string _orQuery = string.Empty;
    private string _notQuery = string.Empty;
    private string _adjQuery = string.Empty;
    private string _nearQuery = string.Empty;
    private readonly QueryExecutor _executor = new();

    [ParamsSource(nameof(AllCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> AllCases() => BenchCorpusCase.MainCases();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        var queries = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus);
        _andQuery = queries[0];
        _orQuery = queries.Length > 1 ? queries[1] : "alpha OR beta";
        _notQuery = queries.Length > 2 ? queries[2] : "alpha AND NOT delta";
        _adjQuery = queries.Length > 3 ? queries[3] : "alpha ADJ beta";
        _nearQuery = queries.Length > 4 ? queries[4] : "alpha NEAR/3 beta";
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int AndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _andQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int OrQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _orQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int NotQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _notQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int AdjQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _adjQuery).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int NearQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _nearQuery).Matches.DocumentIds.Count;
        }

        return total;
    }
}
