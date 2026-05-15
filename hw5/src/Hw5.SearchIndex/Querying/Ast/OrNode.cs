namespace Hw5.SearchIndex.Querying.Ast;

public sealed class OrNode : BinaryNode
{
    public OrNode(QueryNode left, QueryNode right)
        : base(left, right)
    {
    }

    public override MatchSet Evaluate(QueryExecutionContext context)
    {
        var left = Left.Evaluate(context);
        var right = Right.Evaluate(context);
        return left.Or(right);
    }
}
