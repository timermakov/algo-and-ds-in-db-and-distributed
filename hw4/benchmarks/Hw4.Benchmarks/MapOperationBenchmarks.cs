using BenchmarkDotNet.Attributes;
using Hw4.ConcurrentMap;
using System.Collections.Concurrent;

namespace Hw4.Benchmarks;

// Не sealed: BenchmarkDotNet генерирует наследника для прогона.

public class OperationPutBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [ParamsSource(nameof(ThreadCounts))]
    public int Threads { get; set; }

    public static IEnumerable<int> ThreadCounts() => BenchRuntime.ThreadCountsForParams();

    [GlobalSetup]
    public void SeedKeys()
    {
        var n = BenchRuntime.Current.KeySpace;
        _keys = new string[n];
        for (var i = 0; i < n; i++)
            _keys[i] = $"{i:x8}";
    }

    [IterationSetup]
    public void FreshMaps()
    {
        var n = BenchRuntime.Current.KeySpace;
        _custom = new ConcurrentHashTable<string, string>(initialCapacity: n);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: n);
    }

    [IterationCleanup]
    public void DropCustom()
    {
        _custom?.Dispose();
        _custom = null;
    }

    [Benchmark(Baseline = true)]
    public void Put_Custom()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _custom!;
        var seq = 0;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 11003);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 1315423911, keys.Length);
                map.Put(keys[idx], $"{Interlocked.Increment(ref seq)}");
            }
        });
    }

    [Benchmark]
    public void Put_ConcurrentDictionary()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _builtIn!;
        var seq = 0;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 11003);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 1315423911, keys.Length);
                map[keys[idx]] = $"{Interlocked.Increment(ref seq)}";
            }
        });
    }
}

public class OperationGetBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [ParamsSource(nameof(ThreadCounts))]
    public int Threads { get; set; }

    public static IEnumerable<int> ThreadCounts() => BenchRuntime.ThreadCountsForParams();

    [GlobalSetup]
    public void FillMaps()
    {
        var keySpace = BenchRuntime.Current.KeySpace;
        _keys = new string[keySpace];
        for (var i = 0; i < keySpace; i++)
            _keys[i] = $"{i:x8}";

        _custom = new ConcurrentHashTable<string, string>(initialCapacity: keySpace);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: keySpace);

        foreach (var k in _keys)
        {
            var v = $"v-{k}";
            _custom.Put(k, v);
            _builtIn[k] = v;
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _custom?.Dispose();
        _custom = null;
    }

    [Benchmark(Baseline = true)]
    public void Get_Custom()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _custom!;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 7919);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 1315423911, keys.Length);
                BenchSink.AddHash(map.Get(keys[idx])?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }
        });
    }

    [Benchmark]
    public void Get_ConcurrentDictionary()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _builtIn!;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 7919);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 1315423911, keys.Length);
                map.TryGetValue(keys[idx], out var v);
                BenchSink.AddHash(v?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }
        });
    }
}

public class OperationSizeBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [ParamsSource(nameof(ThreadCounts))]
    public int Threads { get; set; }

    public static IEnumerable<int> ThreadCounts() => BenchRuntime.ThreadCountsForParams();

    [GlobalSetup]
    public void FillMaps()
    {
        var keySpace = BenchRuntime.Current.KeySpace;
        _keys = new string[keySpace];
        for (var i = 0; i < keySpace; i++)
            _keys[i] = $"{i:x8}";

        _custom = new ConcurrentHashTable<string, string>(initialCapacity: keySpace);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: keySpace);

        foreach (var k in _keys)
        {
            var v = $"v-{k}";
            _custom.Put(k, v);
            _builtIn[k] = v;
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _custom?.Dispose();
        _custom = null;
    }

    [Benchmark(Baseline = true)]
    public void Size_Custom()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var map = _custom!;
        Parallel.For(0, Threads, _ =>
        {
            var slice = HashHelpers.OpsSlice(ops, Threads);
            long acc = 0;
            for (var i = 0; i < slice; i++)
                acc += map.Size;

            BenchSink.AddHash((int)(acc >> 16));
        });
    }

    [Benchmark]
    public void Size_ConcurrentDictionary_Count()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var map = _builtIn!;
        Parallel.For(0, Threads, _ =>
        {
            var slice = HashHelpers.OpsSlice(ops, Threads);
            long acc = 0;
            for (var i = 0; i < slice; i++)
                acc += map.Count;

            BenchSink.AddHash((int)(acc >> 16));
        });
    }
}

/// <summary>Один вызов Clear после предзаполнения (без параметра Threads — операция глобальная).</summary>
public class OperationClearBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [GlobalSetup]
    public void SeedKeysAndMaps()
    {
        var n = BenchRuntime.Current.KeySpace;
        _keys = new string[n];
        for (var i = 0; i < n; i++)
            _keys[i] = $"{i:x8}";

        _custom = new ConcurrentHashTable<string, string>(initialCapacity: n);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: n);
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _custom?.Dispose();
        _custom = null;
    }

    [IterationSetup]
    public void RefillAll()
    {
        foreach (var k in _keys!)
        {
            _custom!.Put(k, "x");
            _builtIn![k] = "x";
        }
    }

    [Benchmark(Baseline = true)]
    public void Clear_Custom() => _custom!.Clear();

    [Benchmark]
    public void Clear_ConcurrentDictionary() => _builtIn!.Clear();
}

public class OperationMergeBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [ParamsSource(nameof(ThreadCounts))]
    public int Threads { get; set; }

    public static IEnumerable<int> ThreadCounts() => BenchRuntime.ThreadCountsForParams();

    [GlobalSetup]
    public void SeedKeys()
    {
        var n = BenchRuntime.Current.KeySpace;
        _keys = new string[n];
        for (var i = 0; i < n; i++)
            _keys[i] = $"{i:x8}";
    }

    [IterationSetup]
    public void ResetAndSeed()
    {
        var n = BenchRuntime.Current.KeySpace;
        _custom?.Dispose();
        _custom = new ConcurrentHashTable<string, string>(initialCapacity: n);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: n);

        foreach (var k in _keys!)
        {
            _custom.Put(k, "0");
            _builtIn[k] = "0";
        }
    }

    [IterationCleanup]
    public void DropCustomReference()
    {
        _custom?.Dispose();
        _custom = null;
    }

    [Benchmark(Baseline = true)]
    public void Merge_Custom()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _custom!;
        var seq = 0;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 5023);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 710459891, keys.Length);
                var extra = $"{Interlocked.Increment(ref seq)}";
                map.Merge(keys[idx], extra, static (old, incoming) => old + incoming);
            }
        });
    }

    [Benchmark]
    public void Merge_ConcurrentDictionary_AddOrUpdate()
    {
        var cfg = BenchRuntime.Current;
        var ops = cfg.OpsPerInvocation;
        var keys = _keys!;
        var map = _builtIn!;
        var seq = 0;
        Parallel.For(0, Threads, t =>
        {
            var rnd = HashHelpers.Mix((int)t * 5023);
            for (var i = 0; i < HashHelpers.OpsSlice(ops, Threads); i++)
            {
                var idx = HashHelpers.PositiveMod(rnd + i * 710459891, keys.Length);
                var k = keys[idx];
                var extra = $"{Interlocked.Increment(ref seq)}";
                map.AddOrUpdate(k, extra, (_, old) => old + extra);
            }
        });
    }
}

public class OperationEnumerateBenchmarks
{
    string[]? _keys;
    ConcurrentHashTable<string, string>? _custom;
    ConcurrentDictionary<string, string>? _builtIn;

    [ParamsSource(nameof(ThreadCounts))]
    public int Threads { get; set; }

    public static IEnumerable<int> ThreadCounts() => BenchRuntime.ThreadCountsForParams();

    [GlobalSetup]
    public void FillMaps()
    {
        var keySpace = BenchRuntime.Current.KeySpace;
        _keys = new string[keySpace];
        for (var i = 0; i < keySpace; i++)
            _keys[i] = $"{i:x8}";

        _custom = new ConcurrentHashTable<string, string>(initialCapacity: keySpace);
        _builtIn = new ConcurrentDictionary<string, string>(
            concurrencyLevel: Environment.ProcessorCount,
            capacity: keySpace);

        foreach (var k in _keys)
        {
            var v = $"v-{k}";
            _custom.Put(k, v);
            _builtIn[k] = v;
        }
    }

    [GlobalCleanup]
    public void Teardown()
    {
        _custom?.Dispose();
        _custom = null;
    }

    static int EnumerationRepeats(BenchSettings cfg) =>
        Math.Max(1, cfg.OpsPerInvocation / Math.Max(1, cfg.KeySpace));

    [Benchmark(Baseline = true)]
    public void Enumerate_Custom()
    {
        var cfg = BenchRuntime.Current;
        var repeats = EnumerationRepeats(cfg);
        var map = _custom!;
        Parallel.For(0, Threads, _ =>
        {
            var acc = 0;
            for (var r = 0; r < repeats; r++)
            {
                foreach (var kv in map)
                    acc += kv.Key.GetHashCode(StringComparison.Ordinal);
            }

            BenchSink.AddHash(acc);
        });
    }

    [Benchmark]
    public void Enumerate_ConcurrentDictionary()
    {
        var cfg = BenchRuntime.Current;
        var repeats = EnumerationRepeats(cfg);
        var map = _builtIn!;
        Parallel.For(0, Threads, _ =>
        {
            var acc = 0;
            for (var r = 0; r < repeats; r++)
            {
                foreach (var kv in map)
                    acc += kv.Key.GetHashCode(StringComparison.Ordinal);
            }

            BenchSink.AddHash(acc);
        });
    }
}
