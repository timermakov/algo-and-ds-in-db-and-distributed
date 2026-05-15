using Hw5.SearchIndex.Querying.Ast;

namespace Hw5.SearchIndex.Querying;

public sealed class QueryExecutionResult
{
    public QueryExecutionResult(QueryNode ast, MatchSet matches)
    {
        Ast = ast;
        Matches = matches;
    }

    public QueryNode Ast { get; }

    public MatchSet Matches { get; }
}
