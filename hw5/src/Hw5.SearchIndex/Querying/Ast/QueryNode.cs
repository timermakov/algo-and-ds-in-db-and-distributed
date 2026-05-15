namespace Hw5.SearchIndex.Querying.Ast;

public abstract class QueryNode
{
    public abstract MatchSet Evaluate(QueryExecutionContext context);

    public abstract void CollectPositiveTerms(ISet<string> terms, bool underNegation);
}
