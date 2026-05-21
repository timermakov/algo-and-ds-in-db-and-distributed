using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using Hw5.SearchIndex.Corpus;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;

var root = FindHw5Root();
var rawPath = Path.Combine(root, "data", "raw", "enwiki-pages-articles1.xml-p1p41242.bz2");
var outPath = Path.Combine(root, "data", "processed", "docs.jsonl");
var manifestPath = Path.Combine(root, "data", "dataset.manifest.json");
var maxDocs = ParseIntArg(args, "--max", 40000);
var queriesPath = Path.Combine(root, "data", "queries", "wiki-bench-queries.txt");
var useStopWords = !HasFlag(args, "--no-stopwords");
var stopWordsPath = GetArg(args, "--stopwords-file") ?? StopWordFilter.DefaultPath(root);
HashSet<string>? stopWords = useStopWords ? StopWordFilter.LoadFromFile(stopWordsPath) : null;

if (HasFlag(args, "--queries-only"))
{
    RegenerateQueriesFromJsonl(outPath, queriesPath, stopWords);
    return;
}

if (HasFlag(args, "--help"))
{
    Console.WriteLine("Usage: Hw5.CorpusPrep [--max N] [--queries-only] [--no-stopwords] [--stopwords-file path]");
    Console.WriteLine("  --no-stopwords       top-DF query terms include stopwords (the, and, ...)");
    Console.WriteLine("  --stopwords-file     default: data/stopwords-en.txt");
    return;
}

if (!File.Exists(rawPath))
{
    Console.Error.WriteLine($"Missing raw dump: {rawPath}");
    Console.Error.WriteLine("Run: make download-wiki");
    Environment.Exit(1);
}

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(queriesPath)!);

if (useStopWords)
{
    Console.WriteLine($"Stopwords: {stopWords!.Count} terms from {stopWordsPath}");
}
else
{
    Console.WriteLine("Stopwords: disabled (--no-stopwords)");
}

var termDf = new Dictionary<string, int>(StringComparer.Ordinal);
var written = 0;

await using (var fileStream = File.OpenRead(rawPath))
await using (var bz2 = new BZip2Stream(fileStream, CompressionMode.Decompress, false))
await using (var writer = new StreamWriter(File.Create(outPath), new UTF8Encoding(false)))
{
    using var reader = XmlReader.Create(bz2, new XmlReaderSettings { Async = true, IgnoreWhitespace = true });
    var inPage = false;
    var ns = -1;
    var title = string.Empty;
    var pageId = 0;
    var textBuilder = new StringBuilder();

    while (await reader.ReadAsync())
    {
        switch (reader.NodeType)
        {
            case XmlNodeType.Element when reader.Name == "page":
                inPage = true;
                ns = -1;
                title = string.Empty;
                pageId = 0;
                textBuilder.Clear();
                break;
            case XmlNodeType.Element when inPage && reader.Name == "ns":
                if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                {
                    _ = int.TryParse(reader.Value, out ns);
                }

                break;
            case XmlNodeType.Element when inPage && reader.Name == "id" && reader.Depth > 2:
                if (pageId == 0 && await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                {
                    _ = int.TryParse(reader.Value, out pageId);
                }

                break;
            case XmlNodeType.Element when inPage && reader.Name == "title":
                if (await reader.ReadAsync() && reader.NodeType == XmlNodeType.Text)
                {
                    title = reader.Value ?? string.Empty;
                }

                break;
            case XmlNodeType.Element when inPage && reader.Name == "text":
                if (await reader.ReadAsync())
                {
                    textBuilder.Append(reader.Value);
                }

                break;
            case XmlNodeType.EndElement when reader.Name == "page" && inPage:
                if (ns == 0 && pageId > 0)
                {
                    var plain = WikiTextNormalizer.ToPlainText(textBuilder.ToString());
                    if (plain.Length >= 40)
                    {
                        var record = new WikipediaDocumentRecord(pageId, title, plain);
                        await writer.WriteLineAsync(JsonSerializer.Serialize(record));
                        CountTerms(plain, termDf);
                        written++;
                        if (written >= maxDocs)
                        {
                            goto done;
                        }
                    }
                }

                inPage = false;
                break;
        }
    }

done:;
}

var sha256 = await ComputeSha256Async(rawPath);
WriteManifest(manifestPath, rawPath, sha256, written);
WriteQueries(queriesPath, termDf, stopWords);

Console.WriteLine($"Prepared {written} documents -> {outPath}");
Console.WriteLine($"Manifest -> {manifestPath}");
Console.WriteLine($"Queries -> {queriesPath}");

static void CountTerms(string text, Dictionary<string, int> termDf)
{
    var token = new StringBuilder();
    foreach (var ch in text)
    {
        if (char.IsLetterOrDigit(ch))
        {
            token.Append(char.ToLowerInvariant(ch));
            continue;
        }

        if (token.Length > 2)
        {
            var term = token.ToString();
            termDf.TryGetValue(term, out var count);
            termDf[term] = count + 1;
        }

        token.Clear();
    }

    if (token.Length > 2)
    {
        var term = token.ToString();
        termDf.TryGetValue(term, out var count);
        termDf[term] = count + 1;
    }
}

static void RegenerateQueriesFromJsonl(string jsonlPath, string queriesPath, HashSet<string>? stopWords)
{
    if (!File.Exists(jsonlPath))
    {
        Console.Error.WriteLine($"Missing {jsonlPath}");
        Environment.Exit(1);
    }

    var termDf = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var record in WikipediaJsonlReader.ReadRecords(jsonlPath))
    {
        CountTerms(record.Text, termDf);
    }

    Directory.CreateDirectory(Path.GetDirectoryName(queriesPath)!);
    WriteQueries(queriesPath, termDf, stopWords);
    Console.WriteLine($"Queries -> {queriesPath}");
}

static void WriteQueries(string path, Dictionary<string, int> termDf, HashSet<string>? stopWords)
{
    var top = termDf
        .Where(kv => kv.Key.Length > 3 && !StopWordFilter.IsStopWord(kv.Key, stopWords))
        .OrderByDescending(static kv => kv.Value)
        .Select(static kv => kv.Key)
        .Take(12)
        .ToArray();
    if (top.Length < 4)
    {
        return;
    }

    var lines = new List<string>
    {
        $"{top[0]} AND {top[1]}",
        $"{top[0]} OR {top[2]}",
        $"{top[0]} AND NOT {top[3]}",
        $"{top[0]} ADJ {top[1]}",
        $"{top[0]} NEAR/3 {top[2]}",
        $"({top[0]} AND {top[1]}) OR {top[4]}",
        $"{top[5]} NEAR/2 {top[6]} AND NOT {top[7]}",
    };

    File.WriteAllLines(path, lines, Encoding.UTF8);
}

static void WriteManifest(string path, string rawPath, string sha256, int docCount)
{
    var manifest = new
    {
        sourceUrl = "https://dumps.wikimedia.org/enwiki/latest/enwiki-latest-pages-articles1.xml-p1p41242.bz2",
        shard = "pages-articles1 p1p41242",
        rawFile = Path.GetFileName(rawPath),
        sha256,
        processedDocuments = docCount,
        processedAtUtc = DateTime.UtcNow.ToString("O"),
    };
    File.WriteAllText(path, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task<string> ComputeSha256Async(string filePath)
{
    await using var stream = File.OpenRead(filePath);
    var hash = await SHA256.HashDataAsync(stream);
    return Convert.ToHexString(hash).ToLowerInvariant();
}

static string FindHw5Root()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        if (File.Exists(Path.Combine(dir.FullName, "Hw5.sln")))
        {
            return dir.FullName;
        }

        dir = dir.Parent;
    }

    return Directory.GetCurrentDirectory();
}

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

static bool HasFlag(string[] args, string flag) =>
    args.Any(a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));
