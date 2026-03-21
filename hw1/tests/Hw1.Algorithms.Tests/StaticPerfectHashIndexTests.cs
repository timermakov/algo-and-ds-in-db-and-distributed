using FsCheck.Xunit;
using Hw1.Algorithms.PerfectHashing;

namespace Hw1.Algorithms.Tests;

public sealed class StaticPerfectHashIndexTests
{
    [Property(MaxTest = 40)]
    public bool BuildAndLookupRandomEntries(int[] values)
    {
        var data = values
            .Select((value, idx) => new KeyValuePair<string, long>($"key-{idx}-{Math.Abs(value)}", value))
            .DistinctBy(x => x.Key)
            .Take(500)
            .ToArray();

        if (data.Length == 0)
        {
            return true;
        }

        var index = StaticPerfectHashIndex.Build(data);
        foreach (var pair in data)
        {
            if (!index.TryGet(pair.Key, out var actual) || actual != pair.Value)
            {
                return false;
            }
        }

        return true;
    }

    [Fact]
    public void LookupUnknownKeyReturnsFalse()
    {
        var index = StaticPerfectHashIndex.Build(
        [
            new("alpha", 1),
            new("beta", 2),
        ]);

        var found = index.TryGet("gamma", out _);

        Assert.False(found);
    }

    [Fact]
    public void BuildWithDuplicateKeysThrows()
    {
        var action = () => StaticPerfectHashIndex.Build(
        [
            new("dup", 1),
            new("dup", 2),
        ]);

        Assert.Throws<ArgumentException>(action);
    }
}
