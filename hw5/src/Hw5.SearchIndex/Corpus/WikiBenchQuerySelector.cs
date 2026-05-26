namespace Hw5.SearchIndex.Corpus;

/// <summary>
/// Curated bench queries from term DF: medium-frequency content terms, not wikitext markup.
/// </summary>
public static class WikiBenchQuerySelector
{
    private static readonly HashSet<string> MarkupTerms = new(StringComparer.Ordinal)
    {
        "align", "style", "category", "thumb", "https", "http", "ndash", "mdash",
        "quot", "border", "class", "width", "height", "image", "file", "redirect",
        "colspan", "rowspan", "bgcolor", "center", "left", "right", "template",
        "ref", "refs", "cite", "span", "div", "font", "size", "color", "wiki",
        "wikipedia", "external", "links", "references", "seealso", "stub",
    };

    public static IReadOnlyList<string> BuildQueries(
        IReadOnlyDictionary<string, int> termDf,
        int documentCount,
        HashSet<string>? stopWords)
    {
        if (documentCount <= 0 || termDf.Count == 0)
        {
            return [];
        }

        var minDf = Math.Max(2, (int)(documentCount * 0.02));
        var maxDf = Math.Max(minDf + 1, (int)(documentCount * 0.35));

        var ranked = termDf
            .Where(kv => IsContentTerm(kv.Key, stopWords))
            .Where(kv => kv.Value >= minDf && kv.Value <= maxDf)
            .OrderByDescending(static kv => kv.Value)
            .Select(static kv => kv.Key)
            .ToList();

        if (ranked.Count < 8)
        {
            ranked = termDf
                .Where(kv => IsContentTerm(kv.Key, stopWords))
                .OrderByDescending(static kv => kv.Value)
                .Skip(25)
                .Take(20)
                .Select(static kv => kv.Key)
                .ToList();
        }

        if (ranked.Count < 4)
        {
            return [];
        }

        var andA = ranked[0];
        var andB = ranked[Math.Min(4, ranked.Count - 1)];
        var orTerm = ranked[Math.Min(2, ranked.Count - 1)];
        var notTerm = ranked[Math.Min(6, ranked.Count - 1)];
        var adjB = ranked[Math.Min(1, ranked.Count - 1)];
        var nearTerm = ranked[Math.Min(3, ranked.Count - 1)];
        var extraOr = ranked[Math.Min(5, ranked.Count - 1)];
        var near2A = ranked[Math.Min(7, ranked.Count - 1)];
        var near2B = ranked[Math.Min(8, ranked.Count - 1)];
        var not2 = ranked[Math.Min(9, ranked.Count - 1)];

        return
        [
            $"{andA} AND {andB}",
            $"{andA} OR {orTerm}",
            $"{andA} AND NOT {notTerm}",
            $"{andA} ADJ {adjB}",
            $"{andA} NEAR/3 {nearTerm}",
            $"({andA} AND {andB}) OR {extraOr}",
            $"{near2A} NEAR/2 {near2B} AND NOT {not2}",
        ];
    }

    private static bool IsContentTerm(string term, HashSet<string>? stopWords)
    {
        if (term.Length < 4 || MarkupTerms.Contains(term))
        {
            return false;
        }

        return !StopWordFilter.IsStopWord(term, stopWords);
    }
}
