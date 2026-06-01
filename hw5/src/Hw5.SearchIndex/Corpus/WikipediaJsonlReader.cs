using System.Text.Json;
using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Corpus;

public static class WikipediaJsonlReader
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static IEnumerable<WikipediaDocumentRecord> ReadRecords(
        string processedRootOrFile,
        int maxDocuments = int.MaxValue,
        string? hw5Root = null)
    {
        var count = 0;
        foreach (var file in ResolveSourceFiles(processedRootOrFile, hw5Root))
        {
            foreach (var record in ReadFile(file))
            {
                yield return record;
                count++;
                if (count >= maxDocuments)
                {
                    yield break;
                }
            }

            if (count >= maxDocuments)
            {
                yield break;
            }
        }
    }

    public static InMemoryPositionalIndex BuildIndex(string processedRootOrFile, int maxDocuments, string? hw5Root = null)
    {
        var index = new InMemoryPositionalIndex();
        foreach (var record in ReadRecords(processedRootOrFile, maxDocuments, hw5Root))
        {
            index.AddDocument(new SearchDocument(record.Id, record.Text));
        }

        index.Seal();
        return index;
    }

    public static bool IsAvailable(string processedRootOrFile, string? hw5Root = null) =>
        ProcessedCorpusCatalog.IsCorpusAvailable(ResolveRoot(processedRootOrFile, hw5Root));

    public static IReadOnlyList<string> ResolveSourceFiles(string processedRootOrFile, string? hw5Root = null)
    {
        if (File.Exists(processedRootOrFile)
            && processedRootOrFile.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            return [Path.GetFullPath(processedRootOrFile)];
        }

        if (File.Exists(processedRootOrFile)
            && processedRootOrFile.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return [Path.GetFullPath(processedRootOrFile)];
        }

        var root = ResolveRoot(processedRootOrFile, hw5Root);
        var articles = ProcessedCorpusCatalog.ResolveArticleFiles(root);
        if (articles.Count > 0)
        {
            return articles;
        }

        var docsJsonl = ProcessedCorpusCatalog.DocsJsonlPath(root);
        if (File.Exists(docsJsonl))
        {
            return [docsJsonl];
        }

        return [];
    }

    private static IEnumerable<WikipediaDocumentRecord> ReadFile(string path)
    {
        if (path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
        {
            return ReadJsonlFile(path);
        }

        return ReadArticleJsonFile(path);
    }

    private static IEnumerable<WikipediaDocumentRecord> ReadJsonlFile(string jsonlPath)
    {
        using var stream = File.OpenRead(jsonlPath);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var record = TryDeserialize(line);
            if (record is not null)
            {
                yield return record;
            }
        }
    }

    private static IEnumerable<WikipediaDocumentRecord> ReadArticleJsonFile(string jsonPath)
    {
        WikipediaDocumentRecord? record;
        try
        {
            record = JsonSerializer.Deserialize<WikipediaDocumentRecord>(File.ReadAllText(jsonPath), JsonOpts);
        }
        catch (JsonException)
        {
            yield break;
        }

        if (record is not null && !string.IsNullOrWhiteSpace(record.Text))
        {
            yield return record;
        }
    }

    private static WikipediaDocumentRecord? TryDeserialize(string json)
    {
        try
        {
            var record = JsonSerializer.Deserialize<WikipediaDocumentRecord>(json, JsonOpts);
            if (record is null || string.IsNullOrWhiteSpace(record.Text))
            {
                return null;
            }

            return record;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ResolveRoot(string processedRootOrFile, string? hw5Root)
    {
        if (Directory.Exists(processedRootOrFile))
        {
            return processedRootOrFile;
        }

        if (File.Exists(processedRootOrFile))
        {
            return Path.GetDirectoryName(processedRootOrFile)!;
        }

        if (!string.IsNullOrWhiteSpace(hw5Root))
        {
            return ProcessedCorpusCatalog.ResolveProcessedRoot(processedRootOrFile, hw5Root);
        }

        return processedRootOrFile;
    }
}
