namespace Hw5.SearchIndex.Querying.Ast;

public sealed class AdjacentNode : BinaryNode
{
    public AdjacentNode(QueryNode left, QueryNode right, int distance)
        : base(left, right)
    {
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be positive.");
        }

        Distance = distance;
    }

    public int Distance { get; }

    public override MatchSet Evaluate(QueryExecutionContext context)
    {
        var left = Left.Evaluate(context);
        var right = Right.Evaluate(context);
        return left.AdjacentOrdered(right, Distance);
    }
}
