using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Hw1.Algorithms.PerfectHashing;

namespace Hw1.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class StaticPerfectHashBenchmarks
{
    [Params(10_000, 100_000)]
    public int N;

    private StaticPerfectHashIndex _index = null!;
    private Dictionary<string, long> _baseline = null!;
    private string[] _keys = [];
    private int _cursor;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keys = Enumerable.Range(0, N).Select(i => $"key-{i}").ToArray();
        var data = _keys.Select((k, i) => new KeyValuePair<string, long>(k, i)).ToArray();
        _index = StaticPerfectHashIndex.Build(data);
        _baseline = data.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
        Shuffle(_keys);
    }

    [Benchmark]
    public bool LookupPerfectHash()
    {
        var key = _keys[_cursor++ % _keys.Length];
        return _index.TryGet(key, out _);
    }

    [Benchmark(Baseline = true)]
    public bool LookupDictionary()
    {
        var key = _keys[_cursor++ % _keys.Length];
        return _baseline.TryGetValue(key, out _);
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
