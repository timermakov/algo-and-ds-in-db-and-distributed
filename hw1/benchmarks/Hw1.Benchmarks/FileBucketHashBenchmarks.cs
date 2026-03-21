using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Hw1.Algorithms.FileHashing;

namespace Hw1.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class FileBucketHashBenchmarks
{
    [Params(10_000, 100_000)]
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

        _table = FileBucketHashTable.Open(_filePath, new FileBucketHashOptions(BucketCount: NextPow2(N), SlotsPerBucket: 4));
        _baseline = new Dictionary<ulong, long>(N);
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
        Shuffle(_keys);
    }

    [Benchmark]
    public void InsertFileHash()
    {
        var key = _keys[_cursor++ % _keys.Length];
        try
        {
            _table.Insert(key, (long)key);
        }
        catch (InvalidOperationException)
        {
            _table.Update(key, (long)key);
        }
    }

    [Benchmark(Baseline = true)]
    public void InsertDictionary()
    {
        var key = _keys[_cursor++ % _keys.Length];
        _baseline[key] = (long)key;
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

    private static void Shuffle<T>(T[] array)
    {
        for (var i = array.Length - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
