namespace Hw1.Algorithms.FileHashing;

public sealed record FileBucketHashOptions(
    int BucketCount,
    int SlotsPerBucket,
    bool CreateNew = true)
{
    public void Validate()
    {
        if (BucketCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BucketCount), "Bucket count must be positive.");
        }

        if (SlotsPerBucket <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SlotsPerBucket), "Slots per bucket must be positive.");
        }

        if (BucketCount > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(nameof(BucketCount), "Bucket count is too large.");
        }

        if (SlotsPerBucket > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(SlotsPerBucket), "Slots per bucket is too large.");
        }
    }
}
