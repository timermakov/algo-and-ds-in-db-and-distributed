using Hw5.SearchIndex.Querying.Ast;
using Sprache;

namespace Hw5.SearchIndex.Querying;

public static class SearchQueryParser
{
    private static readonly Parser<string> Term =
        Parse.LetterOrDigit.Or(Parse.Char('_'))
            .AtLeastOnce()
            .Text()
            .Token();

    private static readonly Parser<QueryNode> TermNodeParser =
        from term in Term
        select (QueryNode)new TermNode(term.ToLowerInvariant());

    private static readonly Parser<int> Distance =
        from slash in Parse.Char('/')
        from digits in Parse.Number
        select int.Parse(digits);

    private static Parser<int> OptionalDistance(int fallback) =>
        Distance.Optional().Select(value => value.IsDefined ? value.Get() : fallback).Token();

    private static readonly Parser<Func<QueryNode, QueryNode, QueryNode>> AdjacentOperator =
        Parse.IgnoreCase("ADJ").Text().Token()
            .Then(_ => OptionalDistance(1))
            .Select(distance => (Func<QueryNode, QueryNode, QueryNode>)((left, right) => new AdjacentNode(left, right, distance)));

    private static readonly Parser<Func<QueryNode, QueryNode, QueryNode>> NearOperator =
        Parse.IgnoreCase("NEAR").Text().Token()
            .Then(_ => OptionalDistance(3))
            .Select(distance => (Func<QueryNode, QueryNode, QueryNode>)((left, right) => new NearNode(left, right, distance)));

    private static readonly Parser<Func<QueryNode, QueryNode, QueryNode>> AndOperator =
        Parse.IgnoreCase("AND").Text().Token().Select(_ => (Func<QueryNode, QueryNode, QueryNode>)((left, right) => new AndNode(left, right)));

    private static readonly Parser<Func<QueryNode, QueryNode, QueryNode>> OrOperator =
        Parse.IgnoreCase("OR").Text().Token().Select(_ => (Func<QueryNode, QueryNode, QueryNode>)((left, right) => new OrNode(left, right)));

    public static QueryNode ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new QueryParseException("Пустой запрос недопустим.", new ArgumentException("Query is empty.", nameof(query)));
        }

        try
        {
            return Grammar.End().Parse(query);
        }
        catch (Exception ex)
        {
            throw new QueryParseException($"Ошибка разбора запроса: {ex.Message}", ex);
        }
    }

    private static Parser<QueryNode> Grammar =>
        Parse.Ref(() => ParseOrExpression);

    private static Parser<QueryNode> Primary =>
        (from open in Parse.Char('(').Token()
         from expr in Parse.Ref(() => ParseOrExpression)
         from close in Parse.Char(')').Token()
         select expr).XOr(TermNodeParser);

    private static Parser<QueryNode> Unary =>
        (from not in Parse.IgnoreCase("NOT").Text().Token()
         from operand in Parse.Ref(() => Unary)
         select (QueryNode)new NotNode(operand)).XOr(Primary);

    private static Parser<QueryNode> ParsePositionalExpression =>
        Parse.ChainOperator(AdjacentOperator.Or(NearOperator), Unary, static (op, left, right) => op(left, right));

    private static Parser<QueryNode> ParseAndExpression =>
        Parse.ChainOperator(AndOperator, ParsePositionalExpression, static (op, left, right) => op(left, right));

    private static Parser<QueryNode> ParseOrExpression =>
        Parse.ChainOperator(OrOperator, ParseAndExpression, static (op, left, right) => op(left, right));
}
