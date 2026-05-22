using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Ranking;
using Hw5.SearchIndex.Searching;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class RankingBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private string _query = string.Empty;
    private readonly QueryExecutor _executor = new();
    private readonly SearchService _search = new();

    [ParamsSource(nameof(AllCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> AllCases() => BenchCorpusCase.MainCases();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        _query = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus)[1];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int BooleanOnly()
    {
        var total = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            total += _executor.Execute(_memory, _query).Matches.DocumentIds.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public double TfIdfTop10()
    {
        double sum = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            sum += _search.Search(_memory, _query, topK: 10, rankingMode: RankingMode.TfIdf)
                .Sum(static x => x.Score);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public double Bm25Top10()
    {
        double sum = 0;
        for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
        {
            sum += _search.Search(_memory, _query, topK: 10, rankingMode: RankingMode.Bm25)
                .Sum(static x => x.Score);
        }

        return sum;
    }
}
