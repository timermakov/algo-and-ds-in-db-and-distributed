using FsCheck.Xunit;
using Hw1.Algorithms.FileHashing;

namespace Hw1.Algorithms.Tests;

public sealed class FileBucketHashTableTests
{
    [Property(MaxTest = 40)]
    public bool InsertAndTryGetRandomKeys(ulong[] keys)
    {
        var uniqueKeys = keys.Distinct().Take(200).ToArray();
        if (uniqueKeys.Length == 0)
        {
            return true;
        }

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        using var table = FileBucketHashTable.Open(path, new FileBucketHashOptions(BucketCount: 1024, SlotsPerBucket: 8));

        var expected = new Dictionary<ulong, long>();
        for (var i = 0; i < uniqueKeys.Length; i++)
        {
            var value = i * 7L + 11;
            table.Insert(uniqueKeys[i], value);
            expected[uniqueKeys[i]] = value;
        }

        foreach (var pair in expected)
        {
            if (!table.TryGet(pair.Key, out var actual) || actual != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void UpdateChangesStoredValue()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        using var table = FileBucketHashTable.Open(path, new FileBucketHashOptions(BucketCount: 64, SlotsPerBucket: 8));
        table.Insert(42UL, 10);

        table.Update(42UL, 99);

        Assert.True(table.TryGet(42UL, out var value));
        Assert.Equal(99, value);
    }

    [Fact]
    public void DeleteMarksKeyAsRemoved()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.bin");
        using var table = FileBucketHashTable.Open(path, new FileBucketHashOptions(BucketCount: 64, SlotsPerBucket: 8));
        table.Insert(100UL, 123);

        var deleted = table.Delete(100UL);

        Assert.True(deleted);
        Assert.False(table.TryGet(100UL, out _));
    }
}
