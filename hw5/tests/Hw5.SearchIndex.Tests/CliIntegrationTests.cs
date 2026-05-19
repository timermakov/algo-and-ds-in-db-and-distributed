using Hw5.SearchCli;

namespace Hw5.SearchIndex.Tests;

public sealed class CliIntegrationTests
{
    private static string RunScript(IEnumerable<string> lines, bool appendExit = true)
    {
        var scriptLines = appendExit ? lines.Append(":exit") : lines;
        var script = string.Join(Environment.NewLine, scriptLines);
        using var input = new StringReader(script);
        using var output = new StringWriter();
        using var repl = new SearchCliRepl(input, output);
        repl.Run();
        return output.ToString();
    }

    [Fact]
    public void ReplHandlesCommandsAndQueries()
    {
        var segmentPath = Path.Combine(Path.GetTempPath(), $"hw5-cli-{Guid.NewGuid():N}.seg");
        try
        {
            var text = RunScript(
            [
                ":add 1 alpha beta alpha",
                ":add 2 beta gamma",
                ":mode tfidf",
                ":topk 1",
                "alpha OR beta",
                $":save {segmentPath}",
                $":load {segmentPath}",
                "alpha ADJ beta",
            ]);

            Assert.Contains("Добавлен документ 1.", text);
            Assert.Contains("mode = TfIdf", text);
            Assert.Contains("topK = 1", text);
            Assert.Contains("Сегмент сохранен", text);
            Assert.Contains("Сегмент загружен", text);
            Assert.Contains("1\t", text);
        }
        finally
        {
            if (File.Exists(segmentPath))
            {
                File.Delete(segmentPath);
            }
        }
    }

    [Fact]
    public void ReplValidatesTrapInputs()
    {
        var text = RunScript(
        [
            ":add -1 text",
            ":topk 0",
            ":mode unknown",
            ":add 1 alpha beta",
            "alpha AND",
        ]);

        Assert.Contains("id должен быть неотрицательным", text);
        Assert.Contains("положительное целое", text);
        Assert.Contains("режим должен быть bm25 или tfidf", text);
        Assert.Contains("Ошибка запроса", text);
    }

    [Theory]
    [InlineData(":help", "добавить документ")]
    [InlineData(":stats", "Документов:")]
    [InlineData(":build", "зафиксирован")]
    [InlineData(":mode bm25", "mode = Bm25")]
    [InlineData(":topk 5", "topK = 5")]
    [InlineData(":add 10 one two three", "Добавлен документ 10")]
    [InlineData("alpha", "1\t")]
    [InlineData("alpha OR beta", "1\t")]
    [InlineData("alpha AND beta", "1\t")]
    [InlineData("alpha NEAR/2 beta", "1\t")]
    [InlineData("alpha ADJ beta", "1\t")]
    [InlineData(":add", "используйте :add")]
    [InlineData(":save", "используйте :save")]
    [InlineData(":load", "используйте :load")]
    [InlineData(":mode", "используйте :mode")]
    [InlineData(":topk", "используйте :topk")]
    [InlineData(":unknown", "Неизвестная команда")]
    [InlineData(":add x text", "неотрицательным")]
    [InlineData(":add 1", "используйте :add")]
    [InlineData(":topk -3", "положительное целое")]
    [InlineData(":topk abc", "положительное целое")]
    [InlineData(":mode BM25", "mode = Bm25")]
    [InlineData(":mode TFIDF", "mode = TfIdf")]
    [InlineData("   ", "")]
    [InlineData("", "")]
    public void ReplCornerCases(string line, string expectedFragment)
    {
        string[] script = [":add 1 alpha beta gamma", ":build", line];
        var text = RunScript(script);
        if (expectedFragment.Length > 0)
        {
            Assert.Contains(expectedFragment, text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("alpha NEAR/0 beta", "Ошибка запроса")]
    [InlineData("(alpha OR beta", "Ошибка запроса")]
    [InlineData("AND alpha", "Ошибка запроса")]
    public void ReplRejectsMalformedQueries(string query, string expected)
    {
        var text = RunScript([":add 1 alpha beta", ":build", query]);
        Assert.Contains(expected, text);
    }

    [Fact]
    public void ReplSaveLoadRoundTrip()
    {
        var segmentPath = Path.Combine(Path.GetTempPath(), $"hw5-cli-rt-{Guid.NewGuid():N}.seg");
        try
        {
            var text = RunScript(
            [
                ":add 1 alpha beta",
                ":add 2 beta gamma",
                ":build",
                $":save {segmentPath}",
                $":load {segmentPath}",
                "beta",
            ]);
            Assert.Contains("Сегмент сохранен", text);
            Assert.Contains("Сегмент загружен", text);
            Assert.Contains("\t", text);
        }
        finally
        {
            if (File.Exists(segmentPath))
            {
                File.Delete(segmentPath);
            }
        }
    }

    [Fact]
    public void ReplExitCommandsStopLoop()
    {
        var text = RunScript([":add 1 alpha", ":quit"], appendExit: false);
        Assert.DoesNotContain("Ошибка", text);
    }

    [Fact]
    public void ReplEmptyIndexQueryShowsHint()
    {
        var text = RunScript(["alpha"]);
        Assert.Contains("Индекс пуст", text);
    }
}
