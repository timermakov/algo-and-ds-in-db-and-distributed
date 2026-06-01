using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Corpus;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class OperatorBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private string[] _andQueries = [];
    private string[] _orQueries = [];
    private string[] _notQueries = [];
    private string[] _adjQueries = [];
    private string[] _nearQueries = [];
    private readonly QueryExecutor _executor = new();

    [ParamsSource(nameof(AllCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> AllCases() => BenchCorpusCase.MainCases();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        var suite = CorpusBenchmarkBuilder.LoadQuerySuite(Case.Corpus);
        _andQueries = suite.And.ToArray();
        _orQueries = suite.Or.ToArray();
        _notQueries = suite.Not.ToArray();
        _adjQueries = suite.Adj.ToArray();
        _nearQueries = suite.Near.ToArray();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int AndQuery() => ExecuteRotating(_andQueries);

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int OrQuery() => ExecuteRotating(_orQueries);

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int NotQuery() => ExecuteRotating(_notQueries);

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int AdjQuery() => ExecuteRotating(_adjQueries);

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int NearQuery() => ExecuteRotating(_nearQueries);

    private int ExecuteRotating(string[] queries)
    {
        if (queries.Length == 0)
        {
            return 0;
        }

        var total = 0;
        var ops = BenchRuntime.Current.OperationsPerInvoke;
        for (var i = 0; i < ops; i++)
        {
            var query = queries[i % queries.Length];
            total += _executor.Execute(_memory, query).Matches.DocumentIds.Count;
        }

        return total;
    }
}
