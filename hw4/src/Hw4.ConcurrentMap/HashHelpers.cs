namespace Hw4.ConcurrentMap;

/// <summary>
/// Shared hashing helpers (stripe mixing mirrors striping intent from JDK-style concurrent maps).
/// </summary>
internal static class HashHelpers
{
    internal static int StripeIndex(int positiveHash, int stripeMask) =>
        ((positiveHash >>> 11) ^ positiveHash) & stripeMask;
}
