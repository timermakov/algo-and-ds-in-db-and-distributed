namespace Hw5.SearchIndex.Querying.Ast;

public abstract class BinaryNode : QueryNode
{
    protected BinaryNode(QueryNode left, QueryNode right)
    {
        Left = left;
        Right = right;
    }

    public QueryNode Left { get; }

    public QueryNode Right { get; }

    public override void CollectPositiveTerms(ISet<string> terms, bool underNegation)
    {
        Left.CollectPositiveTerms(terms, underNegation);
        Right.CollectPositiveTerms(terms, underNegation);
    }
}
