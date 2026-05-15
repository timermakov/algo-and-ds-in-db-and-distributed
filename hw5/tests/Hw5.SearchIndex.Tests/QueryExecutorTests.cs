using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;

namespace Hw5.SearchIndex.Tests;

public sealed class QueryExecutorTests
{
    private readonly QueryExecutor _executor = new();

    [Fact]
    public void SupportsAndOrNotPrecedence()
    {
        var index = BuildIndex(
            new SearchDocument(1, "alpha beta"),
            new SearchDocument(2, "alpha gamma"),
            new SearchDocument(3, "gamma delta"));

        var result = _executor.Execute(index, "alpha AND NOT gamma OR delta");
        Assert.Equal([1, 3], result.Matches.SortedDocumentIds());
    }

    [Fact]
    public void SupportsAdjAndNearOperators()
    {
        var index = BuildIndex(
            new SearchDocument(1, "alpha beta gamma"),
            new SearchDocument(2, "beta alpha gamma"),
            new SearchDocument(3, "alpha x x beta"));

        var adj = _executor.Execute(index, "alpha ADJ beta");
        Assert.Equal([1], adj.Matches.SortedDocumentIds());

        var near = _executor.Execute(index, "alpha NEAR/2 beta");
        Assert.Equal([1, 2], near.Matches.SortedDocumentIds());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("alpha AND")]
    [InlineData("(alpha OR beta")]
    [InlineData("alpha NEAR/0 beta")]
    [InlineData("AND alpha beta")]
    public void RejectsTrapInputs(string query)
    {
        var index = BuildIndex(new SearchDocument(1, "alpha"));
        Assert.Throws<QueryParseException>(() => _executor.Execute(index, query));
    }

    private static InMemoryPositionalIndex BuildIndex(params SearchDocument[] docs)
    {
        var index = new InMemoryPositionalIndex();
        foreach (var doc in docs)
        {
            index.AddDocument(doc);
        }

        index.Seal();
        return index;
    }
}
