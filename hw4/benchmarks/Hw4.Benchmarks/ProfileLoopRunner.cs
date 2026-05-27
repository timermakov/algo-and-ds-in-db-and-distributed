using Hw4.ConcurrentMap;

namespace Hw4.Benchmarks;

/// <summary>
/// Tight CPU loop without BenchmarkDotNet waits — for dotnet-trace / Speedscope only.
/// </summary>
internal static class ProfileLoopRunner
{
    public static int Run(string[] args)
    {
        var mode = "put";
        var threads = 1;
        var seconds = 30;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (a is "--threads" or "-t" && i + 1 < args.Length)
                threads = int.Parse(args[++i]);
            else if (a is "--seconds" or "-s" && i + 1 < args.Length)
                seconds = int.Parse(args[++i]);
            else if (!a.StartsWith('-'))
                mode = a.Trim().ToLowerInvariant();
        }

        var cfg = BenchRuntime.Current;
        var keySpace = cfg.KeySpace;
        var keys = new string[keySpace];
        for (var i = 0; i < keySpace; i++)
            keys[i] = $"{i:x8}";

        Console.WriteLine($"[Hw4 profile-loop] mode={mode} threads={threads} seconds={seconds} keySpace={keySpace}");

        return mode switch
        {
            "put" => RunPut(keys, threads, seconds),
            "merge" => RunMerge(keys, threads, seconds),
            "get" => RunGet(keys, threads, seconds),
            _ => throw new ArgumentException($"Unknown mode: {mode}. Use put, merge, or get.")
        };
    }

    static int RunPut(string[] keys, int threads, int seconds)
    {
        using var map = new ConcurrentHashTable<string, string>(initialCapacity: keys.Length);
        var end = Environment.TickCount64 + seconds * 1000L;
        var seq = 0;
        Parallel.For(0, threads, t =>
        {
            var rnd = HashHelpers.Mix(t * 11003);
            var i = 0;
            while (Environment.TickCount64 < end)
            {
                var idx = HashHelpers.PositiveMod(rnd + i++ * 1315423911, keys.Length);
                map.Put(keys[idx], $"{Interlocked.Increment(ref seq)}");
            }
        });
        Console.WriteLine($"[Hw4 profile-loop] Put done, size={map.Size}");
        return 0;
    }

    static int RunMerge(string[] keys, int threads, int seconds)
    {
        using var map = new ConcurrentHashTable<string, string>(initialCapacity: keys.Length);
        foreach (var k in keys)
            map.Put(k, "0");

        var end = Environment.TickCount64 + seconds * 1000L;
        var seq = 0;
        Parallel.For(0, threads, t =>
        {
            var rnd = HashHelpers.Mix(t * 5023);
            var i = 0;
            while (Environment.TickCount64 < end)
            {
                var idx = HashHelpers.PositiveMod(rnd + i++ * 710459891, keys.Length);
                var extra = $"{Interlocked.Increment(ref seq)}";
                map.Merge(keys[idx], extra, static (old, inc) => old + inc);
            }
        });
        return 0;
    }

    static int RunGet(string[] keys, int threads, int seconds)
    {
        using var map = new ConcurrentHashTable<string, string>(initialCapacity: keys.Length);
        foreach (var k in keys)
            map.Put(k, $"v-{k}");

        var end = Environment.TickCount64 + seconds * 1000L;
        Parallel.For(0, threads, t =>
        {
            var rnd = HashHelpers.Mix(t * 7919);
            var i = 0;
            while (Environment.TickCount64 < end)
            {
                var idx = HashHelpers.PositiveMod(rnd + i++ * 1315423911, keys.Length);
                BenchSink.AddHash(map.Get(keys[idx])?.GetHashCode(StringComparison.Ordinal) ?? 0);
            }
        });
        return 0;
    }
}
