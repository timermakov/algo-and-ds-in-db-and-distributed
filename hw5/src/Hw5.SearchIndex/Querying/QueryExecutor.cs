using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Querying;

public sealed class QueryExecutor
{
    public QueryExecutionResult Execute(InMemoryPositionalIndex index, string query)
    {
        ArgumentNullException.ThrowIfNull(index);
        var ast = SearchQueryParser.ParseQuery(query);
        var context = new QueryExecutionContext(index);
        var matches = ast.Evaluate(context);
        return new QueryExecutionResult(ast, matches);
    }
}
