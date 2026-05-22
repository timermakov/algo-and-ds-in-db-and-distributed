using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class NaiveScanBenchmarks
{
    private InMemoryPositionalIndex _indexed = null!;
    private NaivePositionalIndexReader _naive = null!;
    private string _query = string.Empty;
    private readonly QueryExecutor _executor = new();

    [ParamsSource(nameof(SmallCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> SmallCases() => BenchCorpusCase.SmallOnly();

    [GlobalSetup]
    public void Setup()
    {
        _indexed = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        _naive = CorpusBenchmarkBuilder.BuildNaiveReader(Case.Corpus, Case.DocumentCount);
        _query = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus)[0];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int IndexedAndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_indexed, _query).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int NaiveAndQuery()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_naive, _query).Matches.DocumentIds.Count;
        }

        return total;
    }
}
