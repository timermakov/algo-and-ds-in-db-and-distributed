namespace Hw1.Algorithms.PerfectHashing;

public sealed class StaticPerfectHashIndex
{
    private const ulong SecondaryBaseSeed = 0x1f83d9abfb41bd6bUL;

    private readonly Bucket[] _buckets;
    private readonly ulong _primarySeed;

    private StaticPerfectHashIndex(Bucket[] buckets, ulong primarySeed)
    {
        _buckets = buckets;
        _primarySeed = primarySeed;
    }

    public int Count { get; private init; }

    public static StaticPerfectHashIndex Build(IEnumerable<KeyValuePair<string, long>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var source = entries.ToArray();
        if (source.Length == 0)
        {
            throw new ArgumentException("Entry set must not be empty.", nameof(entries));
        }

        var unique = new Dictionary<string, long>(source.Length, StringComparer.Ordinal);
        foreach (var pair in source)
        {
            if (!unique.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException("Duplicate keys are not allowed.", nameof(entries));
            }
        }

        var bucketCount = unique.Count;
        var primarySeed = 0x6a09e667f3bcc909UL;
        var grouped = new List<BuildEntry>[bucketCount];
        foreach (var pair in unique)
        {
            var primaryHash = Hash(pair.Key, primarySeed);
            var secondaryHash = Mix64(primaryHash ^ SecondaryBaseSeed);
            var index = (int)(primaryHash % (uint)bucketCount);
            grouped[index] ??= [];
            grouped[index].Add(new BuildEntry(pair.Key, pair.Value, primaryHash, secondaryHash));
        }

        var result = new Bucket[bucketCount];
        for (var bucketId = 0; bucketId < bucketCount; bucketId++)
        {
            var group = grouped[bucketId];
            if (group is null || group.Count == 0)
            {
                result[bucketId] = Bucket.Empty;
                continue;
            }

            if (group.Count == 1)
            {
                var entry = group[0];
                result[bucketId] = Bucket.Single(entry.Key, entry.Value, entry.PrimaryHash, entry.SecondaryHash);
                continue;
            }

            result[bucketId] = BuildSecondary(group);
        }

        return new StaticPerfectHashIndex(result, primarySeed) { Count = unique.Count };
    }

    public bool TryGet(string key, out long value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var primaryHash = Hash(key, _primarySeed);
        var secondaryHash = Mix64(primaryHash ^ SecondaryBaseSeed);
        var bucketId = (int)(primaryHash % (uint)_buckets.Length);
        var bucket = _buckets[bucketId];
        return bucket.TryGet(key, primaryHash, secondaryHash, out value);
    }

    private static Bucket BuildSecondary(IReadOnlyList<BuildEntry> group)
    {
        var size = checked(group.Count * group.Count);
        var seed = 0x9e3779b97f4a7c15UL;
        var maxAttempts = 10_000;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var table = new SecondarySlot[size];
            var collision = false;

            foreach (var pair in group)
            {
                var slot = (int)(Mix64(pair.SecondaryHash ^ seed) % (uint)size);
                if (table[slot].HasValue)
                {
                    collision = true;
                    break;
                }

                table[slot] = new SecondarySlot(true, pair.PrimaryHash, pair.Key, pair.Value);
            }

            if (!collision)
            {
                return Bucket.Multi(seed, table);
            }

            seed = Mix64(seed + (ulong)(attempt + 1));
        }

        throw new InvalidOperationException("Could not build a collision-free secondary table.");
    }

    private static ulong Hash(string text, ulong seed)
    {
        var hash = seed ^ 1469598103934665603UL;
        foreach (var ch in text)
        {
            hash ^= (byte)ch;
            hash *= 1099511628211UL;
            hash ^= (byte)(ch >> 8);
            hash *= 1099511628211UL;
        }

        return Mix64(hash);
    }

    private static ulong Mix64(ulong x)
    {
        x ^= x >> 33;
        x *= 0xff51afd7ed558ccdUL;
        x ^= x >> 33;
        x *= 0xc4ceb9fe1a85ec53UL;
        x ^= x >> 33;
        return x;
    }

    private readonly struct Bucket
    {
        private Bucket(
            bool hasSingle,
            string singleKey,
            long singleValue,
            ulong singlePrimaryHash,
            ulong singleSecondaryHash,
            ulong seed,
            SecondarySlot[] table)
        {
            HasSingle = hasSingle;
            SingleKey = singleKey;
            SingleValue = singleValue;
            SinglePrimaryHash = singlePrimaryHash;
            SingleSecondaryHash = singleSecondaryHash;
            Seed = seed;
            Table = table;
        }

        public static Bucket Empty => new(false, string.Empty, 0L, 0UL, 0UL, 0UL, []);

        public bool HasSingle { get; }
        public string SingleKey { get; }
        public long SingleValue { get; }
        public ulong SinglePrimaryHash { get; }
        public ulong SingleSecondaryHash { get; }
        public ulong Seed { get; }
        public SecondarySlot[] Table { get; }

        public static Bucket Single(string key, long value, ulong primaryHash, ulong secondaryHash) =>
            new(true, key, value, primaryHash, secondaryHash, 0UL, []);

        public static Bucket Multi(ulong seed, SecondarySlot[] table) => new(false, string.Empty, 0L, 0UL, 0UL, seed, table);

        public bool TryGet(string key, ulong primaryHash, ulong secondaryHash, out long value)
        {
            if (HasSingle)
            {
                if (SinglePrimaryHash == primaryHash &&
                    SingleSecondaryHash == secondaryHash &&
                    (ReferenceEquals(SingleKey, key) || string.Equals(SingleKey, key, StringComparison.Ordinal)))
                {
                    value = SingleValue;
                    return true;
                }

                value = default;
                return false;
            }

            if (Table.Length == 0)
            {
                value = default;
                return false;
            }

            var slot = (int)(Mix64(secondaryHash ^ Seed) % (uint)Table.Length);
            var stored = Table[slot];
            if (!stored.HasValue ||
                stored.PrimaryHash != primaryHash ||
                (!ReferenceEquals(stored.Key, key) && !string.Equals(stored.Key, key, StringComparison.Ordinal)))
            {
                value = default;
                return false;
            }

            value = stored.Value;
            return true;
        }
    }

    private readonly record struct BuildEntry(string Key, long Value, ulong PrimaryHash, ulong SecondaryHash);

    private readonly struct SecondarySlot
    {
        public SecondarySlot(bool hasValue, ulong primaryHash, string key, long value)
        {
            HasValue = hasValue;
            PrimaryHash = primaryHash;
            Key = key;
            Value = value;
        }

        public bool HasValue { get; }
        public ulong PrimaryHash { get; }
        public string Key { get; }
        public long Value { get; }
    }
}
