namespace Hw4.ConcurrentMap.Tests;

/// <summary>
/// Forces collisions by overriding hash codes while retaining distinct keys.
/// </summary>
internal sealed record CollisionKey(string Key, int FixedHashCode);

internal sealed class CollisionKeyComparer : IEqualityComparer<CollisionKey>
{
    public bool Equals(CollisionKey? x, CollisionKey? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && x.Key == y.Key);

    public int GetHashCode(CollisionKey obj) => obj.FixedHashCode;
}
