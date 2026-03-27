using BenchmarkDotNet.Attributes;
using Hw1.Algorithms.Lsh;

namespace Hw1.Benchmarks;

[Config(typeof(StableBenchmarkConfig))]
public class TextLshBenchmarks
{
    private const int LshOperationsPerInvoke = 1_536;
    private const int FullScanOperationsPerInvoke = 1_024;

    [Params(1_000, 1_292, 1_668, 2_154, 2_783, 3_594, 4_641, 5_995, 7_743, 10_000)]
    public int N;

    private TextLshIndex _index = null!;
    private string[] _queries = [];
    private int _cursor;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _index = new TextLshIndex(new LshConfig(NumHashes: 64, Bands: 8, ShingleSize: 2, SimilarityThreshold: 0.8));
        var docs = BuildDocs(N);
        _index.BuildIndex(docs);
        var fixedQuery = docs[docs.Count / 2].Text;
        _queries = Enumerable.Repeat(fixedQuery, Math.Min(500, docs.Count)).ToArray();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
    }

    [Benchmark(OperationsPerInvoke = LshOperationsPerInvoke)]
    public int QueryLsh()
    {
        var total = 0;
        for (var i = 0; i < LshOperationsPerInvoke; i++)
        {
            var query = _queries[_cursor++ % _queries.Length];
            total += _index.FindDuplicatesLsh(query, threshold: 0.75).Count;
        }

        return total;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = FullScanOperationsPerInvoke)]
    public int QueryFullScan()
    {
        var total = 0;
        for (var i = 0; i < FullScanOperationsPerInvoke; i++)
        {
            var query = _queries[_cursor++ % _queries.Length];
            total += _index.FindDuplicatesFullScan(query, threshold: 0.75).Count;
        }

        return total;
    }

    private static List<TextDocument> BuildDocs(int count)
    {
        var docs = new List<TextDocument>(count);
        for (var i = 0; i < count; i++)
        {
            var baseText = $"distributed database index shard replica quorum latency {i % 257}";
            var maybeMutated = i % 5 == 0
                ? $"{baseText} warm cache locality"
                : $"{baseText} cold start checkpoint";
            docs.Add(new TextDocument($"doc-{i}", maybeMutated));
        }

        return docs;
    }
}
