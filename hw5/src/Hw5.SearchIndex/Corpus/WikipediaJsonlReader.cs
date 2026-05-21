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

    public static IEnumerable<WikipediaDocumentRecord> ReadRecords(string jsonlPath, int maxDocuments = int.MaxValue)
    {
        using var stream = File.OpenRead(jsonlPath);
        using var reader = new StreamReader(stream);
        var count = 0;
        while (reader.ReadLine() is { } line && count < maxDocuments)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var record = JsonSerializer.Deserialize<WikipediaDocumentRecord>(line, JsonOpts);
            if (record is null || string.IsNullOrWhiteSpace(record.Text))
            {
                continue;
            }

            yield return record;
            count++;
        }
    }

    public static InMemoryPositionalIndex BuildIndex(string jsonlPath, int maxDocuments)
    {
        var index = new InMemoryPositionalIndex();
        foreach (var record in ReadRecords(jsonlPath, maxDocuments))
        {
            index.AddDocument(new SearchDocument(record.Id, record.Text));
        }

        index.Seal();
        return index;
    }

    public static bool IsAvailable(string jsonlPath) => File.Exists(jsonlPath);
}
