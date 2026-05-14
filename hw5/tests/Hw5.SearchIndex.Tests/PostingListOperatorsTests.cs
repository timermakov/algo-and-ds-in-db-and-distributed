using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Tests;

public sealed class PostingListOperatorsTests
{
    [Fact]
    public void IntersectUsesSortedLists()
    {
        var left = new PostingList(
        [
            new Posting(1, [0]),
            new Posting(2, [1]),
            new Posting(4, [2]),
            new Posting(6, [3]),
            new Posting(9, [4]),
        ]);

        var right = new PostingList(
        [
            new Posting(2, [0]),
            new Posting(3, [1]),
            new Posting(4, [1]),
            new Posting(6, [2]),
            new Posting(10, [4]),
        ]);

        var actual = PostingListOperators.Intersect(left, right);
        Assert.Equal([2, 4, 6], actual);
    }

    [Fact]
    public void AdjacentOrderedMatchesExactDistance()
    {
        var actual = PostingListOperators.AdjacentOrdered([1, 5, 7], [2, 6, 9], distance: 1);
        Assert.Equal([2, 6], actual);
    }

    [Fact]
    public void NearUnorderedMatchesWithinWindow()
    {
        var actual = PostingListOperators.NearUnordered([1, 8, 20], [3, 15, 19], distance: 2);
        Assert.Equal([3, 20], actual);
    }
}
