using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Storage;

namespace Hw5.SearchIndex.Tests;

public sealed class RandomizedOperatorOracleTests
{
    private static readonly string[] Vocabulary =
    [
        "alpha", "beta", "gamma", "delta", "omega", "zeta",
    ];

    public static IEnumerable<object[]> Seeds() =>
        Enumerable.Range(0, 200).Select(seed => new object[] { 10_000 + seed });

    [Theory]
    [MemberData(nameof(Seeds))]
    public void AndOperator_MemoryMatchesDisk(int seed)
    {
        AssertOperatorParity(seed, static (l, r) => $"{l} AND {r}");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void OrOperator_MemoryMatchesDisk(int seed)
    {
        AssertOperatorParity(seed, static (l, r) => $"{l} OR {r}");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void NotOperator_MemoryMatchesDisk(int seed)
    {
        AssertOperatorParity(seed, static (l, r) => $"{l} AND NOT {r}");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void AdjOperator_MemoryMatchesDisk(int seed)
    {
        AssertOperatorParity(seed, static (l, r) => $"{l} ADJ {r}");
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void NearOperator_MemoryMatchesDisk(int seed)
    {
        var distance = (seed % 3) + 1;
        AssertOperatorParity(seed, (l, r) => $"{l} NEAR/{distance} {r}");
    }

    private static void AssertOperatorParity(int seed, Func<string, string, string> buildQuery)
    {
        var random = new Random(seed);
        var index = new InMemoryPositionalIndex();
        for (var docId = 1; docId <= 40; docId++)
        {
            var length = random.Next(6, 16);
            var terms = Enumerable.Range(0, length).Select(_ => Vocabulary[random.Next(Vocabulary.Length)]);
            index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
        }

        index.Seal();
        var path = Path.Combine(Path.GetTempPath(), $"hw5-oracle-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, index);
            using var disk = new DiskSegmentIndex(path);
            var left = Vocabulary[random.Next(Vocabulary.Length)];
            var right = Vocabulary[random.Next(Vocabulary.Length)];
            var query = buildQuery(left, right);
            var executor = new QueryExecutor();
            var memory = executor.Execute(index, query).Matches.SortedDocumentIds();
            var onDisk = executor.Execute(disk, query).Matches.SortedDocumentIds();
            Assert.Equal(memory, onDisk);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
