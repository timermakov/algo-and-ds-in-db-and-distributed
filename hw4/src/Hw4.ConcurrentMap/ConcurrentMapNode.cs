namespace Hw4.ConcurrentMap;

internal sealed class ConcurrentMapNode<K, V>(K key, V value) where K : notnull where V : class
{
    internal readonly K Key = key;
    internal V _value = value;
    internal ConcurrentMapNode<K, V>? _next;

    internal V Value
    {
        get => Volatile.Read(ref _value)!;
        set => Volatile.Write(ref _value, value);
    }

    internal ConcurrentMapNode<K, V>? Next
    {
        get => Volatile.Read(ref _next);
        set => Volatile.Write(ref _next, value);
    }
}
