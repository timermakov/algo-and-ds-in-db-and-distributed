using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Querying.Ast;

public sealed class TermNode : QueryNode
{
    public TermNode(string term)
    {
        Term = term;
    }

    public string Term { get; }

    public override MatchSet Evaluate(QueryExecutionContext context)
    {
        var postingList = context.Index.GetPostings(Term);
        return MatchSet.FromPostingList(postingList);
    }

    public override void CollectPositiveTerms(ISet<string> terms, bool underNegation)
    {
        if (!underNegation)
        {
            terms.Add(Term);
        }
    }
}
