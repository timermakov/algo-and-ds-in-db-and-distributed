using BenchmarkDotNet.Attributes;
using Hw1.Algorithms.PerfectHashing;

namespace Hw1.Benchmarks;

[Config(typeof(StableBenchmarkConfig))]
public class StaticPerfectHashBenchmarks
{
    private const int PerfectHashOperationsPerInvoke = 1_000_000;
    private const int DictionaryOperationsPerInvoke = 4_194_304;

    [Params(10_000, 12_915, 16_681, 21_544, 27_826, 35_938, 46_416, 59_948, 77_426, 100_000)]
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
    }

    [Benchmark(OperationsPerInvoke = PerfectHashOperationsPerInvoke)]
    public int LookupPerfectHash()
    {
        var found = 0;
        for (var i = 0; i < PerfectHashOperationsPerInvoke; i++)
        {
            var key = _keys[_cursor++ % _keys.Length];
            if (_index.TryGet(key, out _))
            {
                found++;
            }
        }

        return found;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = DictionaryOperationsPerInvoke)]
    public int LookupDictionary()
    {
        var found = 0;
        for (var i = 0; i < DictionaryOperationsPerInvoke; i++)
        {
            var key = _keys[_cursor++ % _keys.Length];
            if (_baseline.TryGetValue(key, out _))
            {
                found++;
            }
        }

        return found;
    }
}
