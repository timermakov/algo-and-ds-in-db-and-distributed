using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Ranking;

public sealed class Ranker
{
    public IReadOnlyList<RankedDocument> Rank(
        IPositionalIndexReader index,
        IReadOnlyCollection<int> candidateDocIds,
        IReadOnlyCollection<string> queryTerms,
        int topK,
        RankingMode mode)
    {
        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK), "TopK must be positive.");
        }

        if (queryTerms.Count == 0 || candidateDocIds.Count == 0)
        {
            return [];
        }

        var uniqueTerms = queryTerms.Select(static t => t.ToLowerInvariant()).Distinct().ToArray();
        var postingByTerm = uniqueTerms.ToDictionary(term => term, index.GetPostings, StringComparer.Ordinal);
        var avgDocLength = Math.Max(1.0, index.AllDocumentIds.Select(index.GetDocumentLength).DefaultIfEmpty(0).Average());
        var scores = new List<RankedDocument>(candidateDocIds.Count);

        foreach (var docId in candidateDocIds)
        {
            var score = mode switch
            {
                RankingMode.TfIdf => ComputeTfIdf(docId, index.DocumentCount, postingByTerm),
                RankingMode.Bm25 => ComputeBm25(docId, index.DocumentCount, index.GetDocumentLength(docId), avgDocLength, postingByTerm),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown ranking mode."),
            };

            scores.Add(new RankedDocument(docId, score));
        }

        return scores.OrderByDescending(static x => x.Score).ThenBy(static x => x.DocumentId).Take(topK).ToArray();
    }

    private static double ComputeTfIdf(int docId, int totalDocs, IReadOnlyDictionary<string, PostingList> postingByTerm)
    {
        var score = 0.0;
        foreach (var posting in postingByTerm.Values)
        {
            if (!posting.TryGetPositionsForDocument(docId, out var positions))
            {
                continue;
            }

            var tf = positions.Count;
            var idf = Math.Log((double)(totalDocs + 1) / (posting.Count + 1)) + 1;
            score += tf * idf;
        }

        return score;
    }

    private static double ComputeBm25(
        int docId,
        int totalDocs,
        int docLength,
        double avgDocLength,
        IReadOnlyDictionary<string, PostingList> postingByTerm)
    {
        const double k1 = 1.2;
        const double b = 0.75;
        var score = 0.0;
        foreach (var posting in postingByTerm.Values)
        {
            if (!posting.TryGetPositionsForDocument(docId, out var positions))
            {
                continue;
            }

            var tf = positions.Count;
            var df = posting.Count;
            var idf = Math.Log(1 + (totalDocs - df + 0.5) / (df + 0.5));
            var denominator = tf + k1 * (1 - b + b * (docLength / avgDocLength));
            score += idf * (tf * (k1 + 1)) / denominator;
        }

        return score;
    }
}
