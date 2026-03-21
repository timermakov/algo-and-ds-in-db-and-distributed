using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Hw1.Algorithms.Lsh;

namespace Hw1.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class TextLshBenchmarks
{
    [Params(1_000, 10_000)]
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
        _queries = docs.Take(Math.Min(500, docs.Count)).Select(x => x.Text).ToArray();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
        Shuffle(_queries);
    }

    [Benchmark]
    public int QueryLsh()
    {
        var query = _queries[_cursor++ % _queries.Length];
        return _index.FindDuplicatesLsh(query, threshold: 0.75).Count;
    }

    [Benchmark(Baseline = true)]
    public int QueryFullScan()
    {
        var query = _queries[_cursor++ % _queries.Length];
        return _index.FindDuplicatesFullScan(query, threshold: 0.75).Count;
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

    private static void Shuffle<T>(T[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
