using BenchmarkDotNet.Attributes;
using Hw1.Algorithms.FileHashing;

namespace Hw1.Benchmarks;

[Config(typeof(StableBenchmarkConfig))]
public class FileBucketHashBenchmarks
{
    private const int FileHashOperationsPerInvoke = 524_288;
    private const int DictionaryOperationsPerInvoke = 16_777_216;

    [Params(10_000, 12_915, 16_681, 21_544, 27_826, 35_938, 46_416, 59_948, 77_426, 100_000)]
    public int N;

    private string _filePath = string.Empty;
    private FileBucketHashTable _table = null!;
    private ulong[] _keys = [];
    private Dictionary<ulong, long> _baseline = null!;
    private int _cursor;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"hw1-file-hash-{Guid.NewGuid():N}.bin");
        _keys = Enumerable.Range(0, N).Select(i => (ulong)(i + 1)).ToArray();

        _table = FileBucketHashTable.Open(_filePath, new FileBucketHashOptions(BucketCount: NextPow2(N * 4), SlotsPerBucket: 16));
        _baseline = new Dictionary<ulong, long>(N);

        foreach (var key in _keys)
        {
            _table.Insert(key, (long)key);
            _baseline[key] = (long)key;
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
    }

    [Benchmark(OperationsPerInvoke = FileHashOperationsPerInvoke)]
    public void InsertFileHash()
    {
        for (var i = 0; i < FileHashOperationsPerInvoke; i++)
        {
            var key = _keys[_cursor++ % _keys.Length];
            _table.Update(key, (long)key + i);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = DictionaryOperationsPerInvoke)]
    public void InsertDictionary()
    {
        for (var i = 0; i < DictionaryOperationsPerInvoke; i++)
        {
            var key = _keys[_cursor++ % _keys.Length];
            _baseline[key] = (long)key + i;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _table.Dispose();
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private static int NextPow2(int n)
    {
        var value = 1;
        while (value < n)
        {
            value <<= 1;
        }

        return value;
    }
}
