using Hw5.SearchCli;

namespace Hw5.SearchIndex.Tests;

public sealed class SearchCliOptionsTests
{
    [Fact]
    public void ParseDefaultsToEmptyIndex()
    {
        var options = SearchCliOptions.Parse([]);
        Assert.False(options.LoadWiki);
        Assert.Equal(SearchCliOptions.DefaultWikiDocuments, options.MaxDocuments);
    }

    [Fact]
    public void ParseWikiCorpusAndMaxDocs()
    {
        var options = SearchCliOptions.Parse(["--corpus", "wiki", "--max-docs", "500"]);
        Assert.True(options.LoadWiki);
        Assert.Equal(500, options.MaxDocuments);
    }

    [Fact]
    public void ParseRejectsNonPositiveMaxDocs()
    {
        Assert.Throws<ArgumentException>(() => SearchCliOptions.Parse(["--corpus", "wiki", "--max-docs", "0"]));
    }
}

public sealed class SearchCliWikiLoadTests
{
    [Fact]
    public void LoadWikiCorpusReadsJsonlRecords()
    {
        var jsonlPath = Path.Combine(Path.GetTempPath(), $"hw5-wiki-{Guid.NewGuid():N}.jsonl");
        try
        {
            File.WriteAllText(
                jsonlPath,
                """
                {"Id":10,"Title":"One","Text":"alpha beta gamma"}
                {"Id":20,"Title":"Two","Text":"delta omega alpha"}
                """);

            using var output = new StringWriter();
            using var repl = new SearchCliRepl(new StringReader(":stats\n:exit"), output);
            repl.LoadWikiCorpus(jsonlPath, 10);
            repl.Run();

            Assert.Contains("Загружен Wikipedia-корпус: 2 документов", output.ToString());
            Assert.Contains("Документов: 2", output.ToString());
        }
        finally
        {
            if (File.Exists(jsonlPath))
            {
                File.Delete(jsonlPath);
            }
        }
    }
}
