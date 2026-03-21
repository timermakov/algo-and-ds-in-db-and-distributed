using Hw1.Algorithms.Lsh;

namespace Hw1.Algorithms.Tests;

public sealed class TextLshIndexTests
{
    [Fact]
    public void LshFindsNearDuplicate()
    {
        var index = new TextLshIndex(new LshConfig(NumHashes: 64, Bands: 8, ShingleSize: 2, SimilarityThreshold: 0.5));
        index.BuildIndex(
        [
            new TextDocument("a", "database systems are fun and fast"),
            new TextDocument("b", "distributed systems are scalable and resilient"),
            new TextDocument("c", "database systems are fun and very fast"),
        ]);

        var matches = index.FindDuplicatesLsh("database systems are fun and super fast", threshold: 0.4);

        Assert.Contains(matches, x => x.Id == "a");
        Assert.Contains(matches, x => x.Id == "c");
    }

    [Fact]
    public void FullScanAndLshContainExactDuplicate()
    {
        var text = "locality cache line page faults and bandwidth";
        var index = new TextLshIndex(new LshConfig());
        index.BuildIndex(
        [
            new TextDocument("doc-1", text),
            new TextDocument("doc-2", "another unrelated sentence"),
        ]);

        var lsh = index.FindDuplicatesLsh(text, threshold: 0.9);
        var full = index.FindDuplicatesFullScan(text, threshold: 0.9);

        Assert.Contains(lsh, x => x.Id == "doc-1");
        Assert.Contains(full, x => x.Id == "doc-1");
    }

    [Fact]
    public void AddDocumentDuplicateIdThrows()
    {
        var index = new TextLshIndex(new LshConfig());
        index.AddDocument(new TextDocument("x", "one"));

        var action = () => index.AddDocument(new TextDocument("x", "two"));

        Assert.Throws<InvalidOperationException>(action);
    }
}
