using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Tests;

public sealed class BootstrapSanityTests
{
    [Fact]
    public void IndexStoresTermPositions()
    {
        var index = new InMemoryPositionalIndex();
        index.AddDocument(new SearchDocument(1, "Alpha beta alpha"));
        index.Seal();

        var postings = index.GetPostings("alpha");
        Assert.Equal(1, postings.Count);
        Assert.Equal(1, postings.DocumentIdAt(0));
        Assert.Equal([0, 2], postings.PositionsAt(0));
    }
}
