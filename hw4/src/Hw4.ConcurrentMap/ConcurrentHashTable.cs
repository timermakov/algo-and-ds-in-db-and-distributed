using System.Collections;

namespace Hw4.ConcurrentMap;

/// <summary>
/// Thread-safe closed-addressing hash map with semantics documented alongside JDK ConcurrentHashMap.
/// Resize/clear take the writer lock — deviation from JDK transfer algorithm (blocks readers briefly).
/// Число полос блокировок может расти при увеличении массива бакетов (до 4096 полос; см. <see cref="MaybeExpandStripes"/>).
/// </summary>
public sealed class ConcurrentHashTable<K, V> : IEnumerable<KeyValuePair<K, V>>, IDisposable
    where K : notnull
    where V : class
{
    readonly IEqualityComparer<K> _comparer;
    object[] _stripes = null!;
    int _stripeMask;

    readonly ReaderWriterLockSlim _rw = new(LockRecursionPolicy.NoRecursion);

    ConcurrentMapNode<K, V>?[] _buckets;
    int _bucketMask;
    double _loadFactor;
    long _mappingCount;
    long _structureVersion;

    const int MinimumCapacity = 16;

    static int RoundDownToPowerOfTwo(int v)
    {
        v = Math.Clamp(v, 16, 4096);
        var n = 1;
        while (n <= v / 2) n <<= 1;
        return n;
    }

    static readonly int StripeCountPower = RoundDownToPowerOfTwo(Environment.ProcessorCount * 4);

    static int Pow2Ceiling(int min)
    {
        var n = MinimumCapacity;
        while (n < min) n <<= 1;
        return n;
    }

    public ConcurrentHashTable(int initialCapacity = MinimumCapacity, double loadFactor = 0.75,
        IEqualityComparer<K>? comparer = null)
    {
        if (initialCapacity < 1) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (loadFactor is <= 0 or > 1) throw new ArgumentOutOfRangeException(nameof(loadFactor));

        _loadFactor = loadFactor;
        _comparer = comparer ?? EqualityComparer<K>.Default;

        var cap = Pow2Ceiling(initialCapacity);
        _buckets = new ConcurrentMapNode<K, V>?[cap];
        _bucketMask = cap - 1;

        ResetStripesToInitialCapacity();
    }

    int StripeFromHash(int hash) => HashHelpers.StripeIndex(hash, _stripeMask);

    int BucketIndex(K key)
    {
        var h = _comparer.GetHashCode(key);
        unchecked
        {
            h &= int.MaxValue;
            return h & _bucketMask;
        }
    }

    public long Size => Interlocked.Read(ref _mappingCount);

    public long EstimatedMappingCount => Size;

    /// <summary>
    /// Lock-free traversal of the bucket chain for the snapshot of <see cref="_buckets"/> at call time.
    /// Weakly consistent with concurrent resize: may read an older table until writers publish a new array.
    /// </summary>
    public V? Get(K key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var table = Volatile.Read(ref _buckets);
        if (table is null)
            return null;

        var mask = table.Length - 1;
        var idx = HashToIndex(key, _comparer, mask);
        for (var n = Volatile.Read(ref table[idx]); n != null; n = Volatile.Read(ref n._next))
        {
            if (_comparer.Equals(n.Key, key))
                return Volatile.Read(ref n._value);
        }

        return null;
    }

    public void Put(K key, V value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        var hash = _comparer.GetHashCode(key) & int.MaxValue;

        while (true)
        {
            if (TryPutOrDiscoverResize(key, value, hash))
                return;

            Resize();
        }
    }

    bool TryPutOrDiscoverResize(K key, V value, int hash)
    {
        var stripe = StripeFromHash(hash);
        _rw.EnterReadLock();
        try
        {
            lock (_stripes[stripe])
            {
                var table = _buckets;
                var idx = BucketIndex(key);

                for (var n = Volatile.Read(ref table![idx]); n != null; n = Volatile.Read(ref n._next))
                {
                    if (!_comparer.Equals(n.Key, key)) continue;

                    Volatile.Write(ref n._value, value);
                    return true;
                }

                var len = table.Length;
                if (Interlocked.Read(ref _mappingCount) + 1 > (long)(_loadFactor * len))
                    return false;

                var head = Volatile.Read(ref table[idx]);
                var node = new ConcurrentMapNode<K, V>(key, value) { Next = head };
                Volatile.Write(ref table[idx], node);
                Interlocked.Increment(ref _mappingCount);
                return true;
            }
        }
        finally
        {
            _rw.ExitReadLock();
        }
    }

    /// <summary>
    /// Atomically merges per key under stripe lock. Do not call back into this map from <paramref name="merger"/>.
    /// </summary>
    public V Merge(K key, V value, Func<V, V, V> merger)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(merger);

        var hash = _comparer.GetHashCode(key) & int.MaxValue;

        while (true)
        {
            if (TryMergeOrDiscoverResize(key, value, merger, hash, out var result))
                return result;

            Resize();
        }
    }

    bool TryMergeOrDiscoverResize(K key, V value, Func<V, V, V> merger, int hash, out V result)
    {
        var stripe = StripeFromHash(hash);
        result = value;
        _rw.EnterReadLock();
        try
        {
            lock (_stripes[stripe])
            {
                var table = _buckets;
                var idx = BucketIndex(key);

                for (var n = Volatile.Read(ref table![idx]); n != null; n = Volatile.Read(ref n._next))
                {
                    if (!_comparer.Equals(n.Key, key)) continue;

                    var merged = merger(Volatile.Read(ref n._value)!, value);
                    ArgumentNullException.ThrowIfNull(merged);
                    Volatile.Write(ref n._value, merged);
                    result = merged;
                    return true;
                }

                var len = table.Length;
                if (Interlocked.Read(ref _mappingCount) + 1 > (long)(_loadFactor * len))
                    return false;

                var head = Volatile.Read(ref table[idx]);
                var node = new ConcurrentMapNode<K, V>(key, value) { Next = head };
                Volatile.Write(ref table[idx], node);
                Interlocked.Increment(ref _mappingCount);
                result = value;
                return true;
            }
        }
        finally
        {
            _rw.ExitReadLock();
        }
    }

    /// <summary>
    /// Weakly consistent: captures a snapshot of the table while holding a read lock, then yields from a list
    /// (no lock held during user code between items). Structural changes after the snapshot are not reflected.
    /// </summary>
    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {
        var batch = new List<KeyValuePair<K, V>>();
        _rw.EnterReadLock();
        try
        {
            var table = _buckets!;
            var len = table.Length;
            for (var i = 0; i < len; i++)
            {
                for (var n = Volatile.Read(ref table[i]); n != null; n = Volatile.Read(ref n._next))
                {
                    var v = Volatile.Read(ref n._value);
                    if (v != null)
                        batch.Add(new KeyValuePair<K, V>(n.Key, v));
                }
            }
        }
        finally
        {
            _rw.ExitReadLock();
        }

        foreach (var kv in batch)
            yield return kv;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Clear()
    {
        _rw.EnterWriteLock();
        try
        {
            var cap = MinimumCapacity;
            Interlocked.Exchange(ref _mappingCount, 0);
            _buckets = new ConcurrentMapNode<K, V>?[cap];
            _bucketMask = cap - 1;
            ResetStripesToInitialCapacity();
            Interlocked.Increment(ref _structureVersion);
        }
        finally
        {
            _rw.ExitWriteLock();
        }
    }

    void Resize()
    {
        _rw.EnterWriteLock();
        try
        {
            var oldTable = _buckets;
            var oldLen = oldTable!.Length;

            // Must match refusal path in Put/Merge: they use (count + 1) > load*len.
            // Using "<=" here made count == floor(load*len) a no-op → infinite Resize/Put loop.
            if (Interlocked.Read(ref _mappingCount) < (long)(_loadFactor * oldLen))
                return;

            var newLen = oldLen << 1;
            var newTable = new ConcurrentMapNode<K, V>?[newLen];
            var newMask = newLen - 1;

            for (var i = 0; i < oldLen; i++)
            {
                var node = Volatile.Read(ref oldTable[i]);
                while (node != null)
                {
                    var next = Volatile.Read(ref node._next);
                    Volatile.Write(ref node._next, null);

                    var idx = HashToIndex(node.Key, _comparer, newMask);
                    var newHead = Volatile.Read(ref newTable[idx]);
                    Volatile.Write(ref node._next, newHead);
                    Volatile.Write(ref newTable[idx], node);

                    node = next;
                }

                Volatile.Write(ref oldTable[i], null);
            }

            Volatile.Write(ref _buckets, newTable);
            _bucketMask = newMask;
            MaybeExpandStripes(newLen);
            Interlocked.Increment(ref _structureVersion);
        }
        finally
        {
            _rw.ExitWriteLock();
        }
    }

    static int HashToIndex(K key, IEqualityComparer<K> comparer, int mask)
    {
        var h = comparer.GetHashCode(key);
        unchecked
        {
            h &= int.MaxValue;
            return h & mask;
        }
    }

    void ResetStripesToInitialCapacity()
    {
        var stripes = StripeCountPower;
        var arr = new object[stripes];
        for (var i = 0; i < stripes; i++)
            arr[i] = new object();

        _stripes = arr;
        _stripeMask = stripes - 1;
    }

    /// <summary>
    /// Вызывается только из <see cref="Resize"/> под write-lock.
    /// Удваиваем число полос, если бакетов уже в 8 раз больше текущего числа полос и не достигнут потолок (4096).
    /// </summary>
    void MaybeExpandStripes(int newBucketCount)
    {
        var cur = _stripeMask + 1;
        if (cur >= 4096)
            return;

        if (newBucketCount < cur << 3)
            return;

        var target = RoundDownToPowerOfTwo(Math.Min(4096, cur << 1));
        if (target <= cur)
            return;

        var arr = new object[target];
        for (var i = 0; i < target; i++)
            arr[i] = new object();

        _stripes = arr;
        _stripeMask = target - 1;
    }

    internal int StripeCountForTests()
    {
        _rw.EnterReadLock();
        try
        {
            return _stripeMask + 1;
        }
        finally
        {
            _rw.ExitReadLock();
        }
    }

    internal int BucketCapacityForTests()
    {
        _rw.EnterReadLock();
        try
        {
            return _buckets!.Length;
        }
        finally
        {
            _rw.ExitReadLock();
        }
    }

    public void Dispose()
    {
        _rw.Dispose();
        GC.SuppressFinalize(this);
    }
}
