using System.Text.Json;
using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Searching;
using Hw5.SearchIndex.Storage;

if (HasFlag(args, "--compression-stats"))
{
    WriteCompressionStats(ParseIntArg(args, "--docs", 2000));
    return;
}

var docCount = ParseIntArg(args, "--docs", 4000);
var iterations = ParseIntArg(args, "--iterations", 5000);
var query = GetArg(args, "--query") ?? "alpha AND beta OR gamma NEAR/2 delta";

var index = new InMemoryPositionalIndex();
var random = new Random(17);
var vocabulary = new[] { "alpha", "beta", "gamma", "delta", "omega", "zeta" };
for (var docId = 1; docId <= docCount; docId++)
{
    var terms = Enumerable.Range(0, 20).Select(_ => vocabulary[random.Next(vocabulary.Length)]);
    index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
}

index.Seal();
var service = new SearchService();
var sw = System.Diagnostics.Stopwatch.StartNew();
for (var i = 0; i < iterations; i++)
{
    _ = service.Search(index, query, topK: 10);
}

sw.Stop();
Console.WriteLine($"Profile loop: docs={docCount}, iterations={iterations}, elapsed={sw.ElapsedMilliseconds} ms");
Console.WriteLine($"Query: {query}");

static int ParseIntArg(string[] args, string name, int fallback)
{
    var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    if (idx < 0 || idx + 1 >= args.Length || !int.TryParse(args[idx + 1], out var value))
    {
        return fallback;
    }

    return value;
}

static string? GetArg(string[] args, string name)
{
    var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
}

static bool HasFlag(string[] args, string name) =>
    args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

static void WriteCompressionStats(int docCount)
{
    var random = new Random(42);
    var vocabulary = new[] { "alpha", "beta", "gamma", "delta", "omega", "zeta", "kappa", "lambda", "sigma", "theta" };
    var index = new InMemoryPositionalIndex();
    for (var docId = 1; docId <= docCount; docId++)
    {
        var terms = Enumerable.Range(0, 24).Select(_ => vocabulary[random.Next(vocabulary.Length)]);
        index.AddDocument(new SearchDocument(docId, string.Join(' ', terms)));
    }

    index.Seal();
    var naiveBytes = EstimateNaivePostingBytes(index);
    var path = Path.Combine(Path.GetTempPath(), $"hw5-compression-{Guid.NewGuid():N}.seg");
    try
    {
        SegmentSerializer.Write(path, index);
        var segmentBytes = new FileInfo(path).Length;
        var payload = new
        {
            documentCount = docCount,
            termsPerDocument = 24,
            naivePostingBytes = naiveBytes,
            segmentFileBytes = segmentBytes,
            compressionRatio = Math.Round(segmentBytes / (double)naiveBytes, 4),
            spaceSavingsPercent = Math.Round((1.0 - segmentBytes / (double)naiveBytes) * 100.0, 2),
        };
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(json);
        var outDir = Path.Combine("reports", "artifacts");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "compression_stats.json"), json);
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static long EstimateNaivePostingBytes(InMemoryPositionalIndex index)
{
    long total = 0;
    foreach (var term in index.Terms)
    {
        var postings = index.GetPostings(term);
        for (var i = 0; i < postings.Count; i++)
        {
            total += sizeof(int);
            total += postings.PositionsAt(i).Count * sizeof(int);
        }
    }

    return total;
}
