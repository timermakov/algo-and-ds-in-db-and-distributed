using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Ranking;

namespace Hw5.SearchIndex.Searching;

public sealed class SearchService
{
    private readonly QueryExecutor _executor = new();
    private readonly Ranker _ranker = new();

    public IReadOnlyList<RankedDocument> Search(
        IPositionalIndexReader index,
        string query,
        int topK = 10,
        RankingMode rankingMode = RankingMode.Bm25)
    {
        ArgumentNullException.ThrowIfNull(index);
        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), "TopK must be positive.");
        }

        var executionResult = _executor.Execute(index, query);
        var terms = new HashSet<string>(StringComparer.Ordinal);
        executionResult.Ast.CollectPositiveTerms(terms, underNegation: false);
        return _ranker.Rank(index, executionResult.Matches.DocumentIds, terms, topK, rankingMode);
    }
}
