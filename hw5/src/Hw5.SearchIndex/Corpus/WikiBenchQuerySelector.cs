namespace Hw5.SearchIndex.Corpus;

/// <summary>
/// Bench queries from corpus term DF: pairs from high / mid / low frequency tiers.
/// AND, OR, ADJ, NEAR on the same (termA, termB) per pair; NOT uses termA AND NOT excludeTerm.
/// </summary>
public static class WikiBenchQuerySelector
{
    public const int AndQueryIndex = 0;
    public const int OrQueryIndex = 1;
    public const int NotQueryIndex = 2;
    public const int AdjQueryIndex = 3;
    public const int NearQueryIndex = 4;
    public const int CompositeOrQueryIndex = 5;
    public const int CompositeNearNotQueryIndex = 6;

    public const int NearDistance = 3;
    public const int PairsPerTier = 3;

    private static readonly HashSet<string> MarkupTerms = new(StringComparer.Ordinal)
    {
        "align", "style", "category", "thumb", "https", "http", "ndash", "mdash",
        "quot", "border", "class", "width", "height", "image", "file", "redirect",
        "colspan", "rowspan", "bgcolor", "center", "left", "right", "template",
        "ref", "refs", "cite", "span", "div", "font", "size", "color", "wiki",
        "wikipedia", "external", "links", "references", "seealso", "stub",
    };

    public static BenchQuerySuite BuildSuite(
        IReadOnlyDictionary<string, int> termDf,
        int documentCount,
        HashSet<string>? stopWords)
    {
        var ranked = RankContentTerms(termDf, stopWords);
        if (ranked.Count < 8)
        {
            return BenchQuerySuite.BuildSynthetic();
        }

        var pairs = BuildTierPairs(ranked);
        return BenchQuerySuite.FromPairs(pairs);
    }

    public static IReadOnlyList<string> BuildQueries(
        IReadOnlyDictionary<string, int> termDf,
        int documentCount,
        HashSet<string>? stopWords) =>
        BuildSuite(termDf, documentCount, stopWords).ToLegacySlots();

    private static List<BenchTermPair> BuildTierPairs(List<string> ranked)
    {
        var pairs = new List<BenchTermPair>(PairsPerTier * 3);
        var excludePool = ranked.Take(Math.Min(12, ranked.Count)).ToList();

        foreach (var (tier, startIndex) in TierOffsets(ranked.Count))
        {
            for (var p = 0; p < PairsPerTier; p++)
            {
                var offset = startIndex + p * 2;
                if (offset + 1 >= ranked.Count)
                {
                    break;
                }

                var termA = ranked[offset];
                var termB = ranked[offset + 1];
                var exclude = PickExcludeTerm(excludePool, termA, termB);
                pairs.Add(new BenchTermPair(tier, termA, termB, exclude));
            }
        }

        return pairs;
    }

    private static IEnumerable<(string Tier, int StartIndex)> TierOffsets(int count)
    {
        yield return ("high", 0);
        yield return ("mid", Math.Max(0, count / 3));
        yield return ("low", Math.Max(0, (count * 2) / 3));
    }

    private static string PickExcludeTerm(IReadOnlyList<string> pool, string termA, string termB)
    {
        foreach (var term in pool)
        {
            if (!string.Equals(term, termA, StringComparison.Ordinal)
                && !string.Equals(term, termB, StringComparison.Ordinal))
            {
                return term;
            }
        }

        return pool[0];
    }

    private static List<string> RankContentTerms(
        IReadOnlyDictionary<string, int> termDf,
        HashSet<string>? stopWords)
    {
        return termDf
            .Where(kv => IsContentTerm(kv.Key, stopWords))
            .OrderByDescending(static kv => kv.Value)
            .ThenBy(static kv => kv.Key, StringComparer.Ordinal)
            .Select(static kv => kv.Key)
            .ToList();
    }

    private static bool IsContentTerm(string term, HashSet<string>? stopWords)
    {
        if (term.Length < 4 || MarkupTerms.Contains(term))
        {
            return false;
        }

        if (term.Any(static c => !char.IsAsciiLetter(c)))
        {
            return false;
        }

        if (term.Any(char.IsDigit))
        {
            return false;
        }

        return !StopWordFilter.IsStopWord(term, stopWords);
    }
}
