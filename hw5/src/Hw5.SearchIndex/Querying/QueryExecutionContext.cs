using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Querying;

public sealed class QueryExecutionContext
{
    public QueryExecutionContext(InMemoryPositionalIndex index)
    {
        Index = index;
        Universe = [.. index.AllDocumentIds.Order()];
    }

    public InMemoryPositionalIndex Index { get; }

    public IReadOnlyList<int> Universe { get; }
}
