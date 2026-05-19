using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Searching;

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
