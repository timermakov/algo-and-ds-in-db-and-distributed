using Hw5.SearchIndex.Corpus;

namespace Hw5.SearchIndex.Tests;

public sealed class WikiBenchQuerySelectorTests
{
    [Fact]
    public void BuildQueries_ExcludesMarkupTerms()
    {
        var df = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["align"] = 4000,
            ["ndash"] = 3500,
            ["style"] = 3000,
            ["history"] = 800,
            ["population"] = 600,
            ["located"] = 550,
            ["county"] = 500,
            ["river"] = 480,
            ["village"] = 460,
            ["district"] = 440,
            ["municipality"] = 420,
            ["century"] = 400,
        };

        var suite = WikiBenchQuerySelector.BuildSuite(df, 5000, stopWords: null);

        Assert.NotEmpty(suite.And);
        Assert.DoesNotContain(suite.And, q => q.Contains("align", StringComparison.Ordinal));
        Assert.StartsWith("history AND", suite.And[0], StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSuite_UsesSameTermPairAcrossBooleanOperators()
    {
        var df = BuildGraduatedDf();
        var suite = WikiBenchQuerySelector.BuildSuite(df, 5000, stopWords: null);

        Assert.Equal(suite.And.Count, suite.Or.Count);
        Assert.Equal(suite.And.Count, suite.Adj.Count);
        Assert.Equal(suite.And.Count, suite.Near.Count);

        for (var i = 0; i < suite.Pairs.Count; i++)
        {
            var pair = suite.Pairs[i];
            Assert.Equal($"{pair.TermA} AND {pair.TermB}", suite.And[i]);
            Assert.Equal($"{pair.TermA} OR {pair.TermB}", suite.Or[i]);
            Assert.Equal($"{pair.TermA} ADJ {pair.TermB}", suite.Adj[i]);
            Assert.Equal($"{pair.TermA} NEAR/{WikiBenchQuerySelector.NearDistance} {pair.TermB}", suite.Near[i]);
            Assert.Equal($"{pair.TermA} AND NOT {pair.ExcludeTerm}", suite.Not[i]);
        }
    }

    [Fact]
    public void BuildSuite_IncludesHighMidLowTiers()
    {
        var suite = WikiBenchQuerySelector.BuildSuite(BuildGraduatedDf(), 5000, stopWords: null);
        var tiers = suite.Pairs.Select(static p => p.Tier).Distinct().OrderBy(static t => t).ToList();
        Assert.Contains("high", tiers);
        Assert.Contains("mid", tiers);
        Assert.Contains("low", tiers);
    }

    private static Dictionary<string, int> BuildGraduatedDf()
    {
        var df = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < 30; i++)
        {
            df[$"term{i:00}"] = 1000 - i * 10;
        }

        return df;
    }
}
