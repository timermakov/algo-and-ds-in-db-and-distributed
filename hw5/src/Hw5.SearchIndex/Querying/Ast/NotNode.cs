namespace Hw5.SearchIndex.Querying.Ast;

public sealed class NotNode : QueryNode
{
    public NotNode(QueryNode inner)
    {
        Inner = inner;
    }

    public QueryNode Inner { get; }

    public override MatchSet Evaluate(QueryExecutionContext context)
    {
        return Inner.Evaluate(context).Not(context.Universe);
    }

    public override void CollectPositiveTerms(ISet<string> terms, bool underNegation)
    {
        Inner.CollectPositiveTerms(terms, underNegation: true);
    }
}
