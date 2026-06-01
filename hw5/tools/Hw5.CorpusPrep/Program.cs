using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using Hw5.SearchIndex.Corpus;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;

var root = FindHw5Root();
var processedRoot = Path.Combine(root, "data", "processed");
var rawPath = Path.Combine(root, "data", "raw", "enwiki-pages-articles1.xml-p1p41242.bz2");
var datasetManifestPath = Path.Combine(root, "data", "dataset.manifest.json");
var queriesTxtPath = Path.Combine(root, "data", "queries", "wiki-bench-queries.txt");
var queriesJsonPath = Path.Combine(root, "data", "queries", "wiki-bench-suite.json");
var maxDocs = ParseIntArg(args, "--max", 40000);
var sampleDocs = ParseIntArg(args, "--sample", 100);
var useStopWords = !HasFlag(args, "--no-stopwords");
var stopWordsPath = GetArg(args, "--stopwords-file") ?? StopWordFilter.DefaultPath(root);
HashSet<string>? stopWords = useStopWords ? StopWordFilter.LoadFromFile(stopWordsPath) : null;

if (HasFlag(args, "--help"))
{
    Console.WriteLine("Usage: Hw5.CorpusPrep [--max N] [--sample 100]");
    Console.WriteLine("  --queries-only       rebuild query suite from processed corpus");
    Console.WriteLine("  --to-articles-only   build articles/ from docs.jsonl");
    Console.WriteLine("  --no-stopwords       include stopwords in DF ranking");
    return;
}

if (HasFlag(args, "--queries-only"))
{
    RegenerateQueriesFromCorpus(processedRoot, queriesTxtPath, queriesJsonPath, stopWords, root);
    return;
}

if (HasFlag(args, "--to-articles-only"))
{
    await ExportCorpusAsArticlesAsync(processedRoot, sampleDocs);
    return;
}

if (!File.Exists(rawPath))
{
    Console.Error.WriteLine($"Missing raw dump: {rawPath}");
    Console.Error.WriteLine("Run: make download-wiki");
    Environment.Exit(1);
}

Directory.CreateDirectory(processedRoot);
Directory.CreateDirectory(Path.GetDirectoryName(queriesTxtPath)!);

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

await using (var articleWriter = new CorpusArticleWriter(processedRoot, sampleDocs))
{
    await using var fileStream = File.OpenRead(rawPath);
    await using var bz2 = new BZip2Stream(fileStream, CompressionMode.Decompress, false);
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
                        await articleWriter.WriteAsync(record);
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

done:
    var corpusManifest = articleWriter.CompleteManifest();
    ProcessedCorpusCatalog.WriteManifest(processedRoot, corpusManifest);
}

var sha256 = await ComputeSha256Async(rawPath);
WriteDatasetManifest(datasetManifestPath, rawPath, sha256, written);
WriteQueryArtifacts(queriesTxtPath, queriesJsonPath, termDf, written, stopWords);

Console.WriteLine($"Prepared {written} documents -> {ProcessedCorpusCatalog.ArticlesDirectory(processedRoot)}");
Console.WriteLine($"Sample (jsonl) -> {ProcessedCorpusCatalog.SamplePath(processedRoot)}");
Console.WriteLine($"Corpus manifest -> {ProcessedCorpusCatalog.ManifestPath(processedRoot)}");
Console.WriteLine($"Dataset manifest -> {datasetManifestPath}");
Console.WriteLine($"Queries -> {queriesJsonPath}");

static async Task ExportCorpusAsArticlesAsync(string processedRoot, int sampleDocs)
{
    var sources = ListMigrationSources(processedRoot);
    if (sources.Count == 0)
    {
        Console.Error.WriteLine("No corpus source found (docs.jsonl or articles/).");
        Environment.Exit(1);
    }

    var articlesDir = ProcessedCorpusCatalog.ArticlesDirectory(processedRoot);
    if (Directory.Exists(articlesDir))
    {
        foreach (var file in Directory.EnumerateFiles(articlesDir, "*.json"))
        {
            File.Delete(file);
        }
    }

    await using var writer = new CorpusArticleWriter(processedRoot, sampleDocs);
    foreach (var source in sources)
    {
        foreach (var record in WikipediaJsonlReader.ReadRecords(source))
        {
            await writer.WriteAsync(record);
        }
    }

    var manifest = writer.CompleteManifest();
    ProcessedCorpusCatalog.WriteManifest(processedRoot, manifest);
    Console.WriteLine($"Exported {manifest.ProcessedDocuments} articles -> {articlesDir}");
}

static List<string> ListMigrationSources(string processedRoot)
{
    var docsJsonl = ProcessedCorpusCatalog.DocsJsonlPath(processedRoot);
    return File.Exists(docsJsonl) ? [docsJsonl] : [];
}

static void RegenerateQueriesFromCorpus(
    string processedRoot,
    string queriesTxtPath,
    string queriesJsonPath,
    HashSet<string>? stopWords,
    string hw5Root)
{
    if (!ProcessedCorpusCatalog.IsCorpusAvailable(processedRoot)
        && !ProcessedCorpusCatalog.IsCorpusAvailable(Path.Combine(hw5Root, "data", "processed")))
    {
        Console.Error.WriteLine("Processed corpus not found. Run: make prepare-corpus");
        Environment.Exit(1);
    }

    var corpusPath = ProcessedCorpusCatalog.IsCorpusAvailable(processedRoot)
        ? processedRoot
        : Path.Combine(hw5Root, "data", "processed");

    var termDf = new Dictionary<string, int>(StringComparer.Ordinal);
    var docCount = 0;
    foreach (var record in WikipediaJsonlReader.ReadRecords(corpusPath, int.MaxValue, hw5Root))
    {
        CountTerms(record.Text, termDf);
        docCount++;
    }

    WriteQueryArtifacts(queriesTxtPath, queriesJsonPath, termDf, docCount, stopWords);
    Console.WriteLine($"Queries from {docCount} documents -> {queriesJsonPath}");
}

static void WriteQueryArtifacts(
    string queriesTxtPath,
    string queriesJsonPath,
    Dictionary<string, int> termDf,
    int documentCount,
    HashSet<string>? stopWords)
{
    var suite = WikiBenchQuerySelector.BuildSuite(termDf, documentCount, stopWords);
    if (suite.And.Count == 0)
    {
        return;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(queriesTxtPath)!);
    BenchQuerySuite.SaveJson(queriesJsonPath, suite, documentCount);
    WriteHumanQueriesFile(queriesTxtPath, suite);
}

static void WriteHumanQueriesFile(string path, BenchQuerySuite suite)
{
    var lines = new List<string>
    {
        "# HW5 wiki bench queries — same (termA, termB) per pair for AND/OR/ADJ/NEAR",
        "# tiers: high / mid / low DF; OperatorBenchmarks cycles all lines per operator",
        "# --- AND ---",
    };
    lines.AddRange(suite.And.Select(static q => $"AND\t{q}"));
    lines.Add("# --- OR ---");
    lines.AddRange(suite.Or.Select(static q => $"OR\t{q}"));
    lines.Add("# --- NOT ---");
    lines.AddRange(suite.Not.Select(static q => $"NOT\t{q}"));
    lines.Add("# --- ADJ ---");
    lines.AddRange(suite.Adj.Select(static q => $"ADJ\t{q}"));
    lines.Add("# --- NEAR ---");
    lines.AddRange(suite.Near.Select(static q => $"NEAR\t{q}"));
    lines.Add("# --- composite ---");
    lines.AddRange(suite.Composite);
    File.WriteAllLines(path, lines, Encoding.UTF8);
}

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

static void WriteDatasetManifest(string path, string rawPath, string sha256, int docCount)
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
