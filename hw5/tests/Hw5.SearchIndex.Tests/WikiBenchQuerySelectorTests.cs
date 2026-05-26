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

        var queries = WikiBenchQuerySelector.BuildQueries(df, 5000, stopWords: null);

        Assert.NotEmpty(queries);
        Assert.DoesNotContain(queries, q => q.Contains("align", StringComparison.Ordinal));
        Assert.DoesNotContain(queries, q => q.Contains("ndash", StringComparison.Ordinal));
        Assert.StartsWith("history AND", queries[0], StringComparison.Ordinal);
    }
}
