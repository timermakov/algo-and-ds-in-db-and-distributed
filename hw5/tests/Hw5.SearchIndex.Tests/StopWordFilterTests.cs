using Hw5.SearchIndex.Corpus;

namespace Hw5.SearchIndex.Tests;

public sealed class StopWordFilterTests
{
    [Fact]
    public void LoadFromFile_skips_comments_and_blank_lines()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-stopwords-{Guid.NewGuid():N}.txt");
        File.WriteAllLines(path, ["# header", "", "The", "and", "align"]);
        try
        {
            var set = StopWordFilter.LoadFromFile(path);
            Assert.Equal(3, set.Count);
            Assert.Contains("the", set);
            Assert.Contains("and", set);
            Assert.Contains("align", set);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void IsStopWord_respects_null_set_as_disabled()
    {
        Assert.False(StopWordFilter.IsStopWord("the", null));
        var set = new HashSet<string>(StringComparer.Ordinal) { "the" };
        Assert.True(StopWordFilter.IsStopWord("the", set));
        Assert.False(StopWordFilter.IsStopWord("align", set));
    }
}
