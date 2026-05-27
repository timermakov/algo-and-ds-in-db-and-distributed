using Xunit;

namespace Hw4.ConcurrentMap.Tests;

[Trait("Category", "Stress")]
public sealed class ConcurrentHashTableStressTests
{
    [Fact]
    public void Parallel_puts_then_reads_are_consistent()
    {
        const int threads = 4;
        const int keysPerThread = 150;

        using var map = new ConcurrentHashTable<int, string>(initialCapacity: 1024);
        var barrier = new Barrier(threads);

        Parallel.For(0, threads, t =>
        {
            barrier.SignalAndWait();
            var offset = (int)t * keysPerThread;
            for (var i = 0; i < keysPerThread; i++)
            {
                var k = offset + i;
                map.Put(k, $"{k}");
            }
        });

        Assert.Equal(threads * keysPerThread, map.Size);

        Parallel.For(0, threads * keysPerThread, k =>
        {
            Assert.Equal($"{k}", map.Get(k));
        });
    }

    [Fact]
    public void Parallel_merge_without_loss_per_distinct_keys()
    {
        const int threads = 4;
        const int iterations = 100;

        using var map = new ConcurrentHashTable<string, string>(initialCapacity: 256);
        var barrier = new Barrier(threads);

        Parallel.For(0, threads, t =>
        {
            barrier.SignalAndWait();
            var key = $"k{t}";
            for (var i = 0; i < iterations; i++)
                map.Merge(key, "a", (a, b) => a + b);
        });

        for (var t = 0; t < threads; t++)
            Assert.Equal(new string('a', iterations), map.Get($"k{t}"));

        Assert.Equal(threads, map.Size);
    }

    [Fact]
    public void Mixed_put_get_under_load()
    {
        const int threads = 4;
        const int rounds = 280;

        using var map = new ConcurrentHashTable<int, string>(initialCapacity: 512);
        var rnd = new Random(91);

        Parallel.For(0, threads, t =>
        {
            var local = new Random(rnd.Next() ^ (int)t);
            for (var r = 0; r < rounds; r++)
            {
                var k = local.Next(96);
                if ((r & 1) == 0)
                    map.Put(k, $"{t}:{r}");
                else
                    _ = map.Get(k);
            }
        });

        Assert.True(map.Size > 0);
        Assert.True(map.Size <= 96);
    }
}
