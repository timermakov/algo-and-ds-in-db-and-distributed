using Xunit;

namespace Hw4.ConcurrentMap.Tests;

public sealed class ConcurrentHashTableFunctionalTests
{
    [Fact]
    public void Put_Get_roundtrip_matches_dictionary_single_thread()
    {
        var rnd = new Random(42);
        var map = new ConcurrentHashTable<string, string>(initialCapacity: 64);
        var golden = new Dictionary<string, string>();

        for (var i = 0; i < 5000; i++)
        {
            var op = rnd.Next(3);
            var key = $"k{rnd.Next(200)}";
            var val = $"v{rnd.Next(10000)}";

            switch (op)
            {
                case 0:
                case 1:
                    map.Put(key, val);
                    golden[key] = val;
                    break;
                default:
                    Assert.Equal(golden.TryGetValue(key, out var gv) ? gv : null, map.Get(key));
                    break;
            }
        }

        foreach (var kv in golden)
            Assert.Equal(kv.Value, map.Get(kv.Key));

        Assert.Equal(golden.Count, map.Size);
    }

    [Fact]
    public void Size_matches_dictionary_after_quiescence()
    {
        using var map = new ConcurrentHashTable<int, string>(initialCapacity: 32);
        var golden = new Dictionary<int, string>();
        var rnd = new Random(7);

        for (var i = 0; i < 800; i++)
        {
            var k = rnd.Next(150);
            var v = $"x{i}";
            map.Put(k, v);
            golden[k] = v;
        }

        Assert.Equal(golden.Count, map.Size);
    }

    [Fact]
    public void Resize_increases_bucket_capacity_under_collision_pressure()
    {
        var cmp = new CollisionKeyComparer();
        using var map = new ConcurrentHashTable<CollisionKey, string>(initialCapacity: 16, loadFactor: 0.75, cmp);

        var cap0 = map.BucketCapacityForTests();

        for (var i = 0; i < 40; i++)
            map.Put(new CollisionKey($"id{i}", FixedHashCode: 42), "v");

        var cap1 = map.BucketCapacityForTests();
        Assert.True(cap1 > cap0);
    }

    [Fact]
    public void Stripe_count_can_grow_with_buckets_and_Clear_resets_stripe_fanout()
    {
        using var map = new ConcurrentHashTable<int, string>(initialCapacity: 16);
        var stripes0 = map.StripeCountForTests();

        for (var i = 0; i < 3500; i++)
            map.Put(i, $"{i}");

        Assert.True(map.BucketCapacityForTests() >= 512);
        Assert.True(map.StripeCountForTests() >= stripes0);

        map.Clear();
        Assert.Equal(stripes0, map.StripeCountForTests());
        Assert.Equal(16, map.BucketCapacityForTests());
    }

    [Fact]
    public void Put_does_not_spin_at_load_factor_exact_boundary()
    {
        // Regression: Pow2Ceiling(64)==64 → threshold floor(load*N)==48; inserting the 49th key
        // must trigger a real Resize, not Resize() returning immediately on "count <= threshold".
        using var map = new ConcurrentHashTable<string, string>(initialCapacity: 64);
        for (var i = 0; i < 200; i++)
            map.Put($"{i:x8}", $"{i}");

        Assert.Equal(200L, map.Size);
    }

    [Fact]
    public void Clear_resets_map()
    {
        using var map = new ConcurrentHashTable<string, string>();
        map.Put("a", "1");
        map.Put("b", "2");
        map.Clear();

        Assert.Null(map.Get("a"));
        Assert.Equal(0, map.Size);
    }

    [Fact]
    public void Merge_accumulates_under_single_thread()
    {
        using var map = new ConcurrentHashTable<string, string>();

        Assert.Equal("x", map.Merge("k", "x", (_, incoming) => incoming));
        Assert.Equal("xb", map.Merge("k", "b", (a, b) => a + b));
        Assert.Equal("xbc", map.Merge("k", "c", (a, b) => a + b));

        Assert.Equal("xbc", map.Get("k"));
        Assert.Equal(1, map.Size);
    }

    [Fact]
    public void Iterator_contains_all_keys_after_quiescence()
    {
        using var map = new ConcurrentHashTable<int, string>();
        var golden = new Dictionary<int, string>();

        for (var i = 0; i < 50; i++)
        {
            map.Put(i, $"{i}");
            golden[i] = $"{i}";
        }

        var seen = new Dictionary<int, string>();
        foreach (var kv in map)
            seen[kv.Key] = kv.Value;

        Assert.Equal(golden.Count, seen.Count);
        foreach (var kv in golden)
            Assert.Equal(kv.Value, seen[kv.Key]);
    }

    [Fact]
    public void Get_returns_null_when_missing()
    {
        using var map = new ConcurrentHashTable<string, string>();
        Assert.Null(map.Get("missing"));
    }

    [Fact]
    public void Put_throws_on_null_arguments()
    {
        using var map = new ConcurrentHashTable<string, string>();
        Assert.Throws<ArgumentNullException>(() => map.Put(null!, "a"));
        Assert.Throws<ArgumentNullException>(() => map.Put("a", null!));
    }
}
