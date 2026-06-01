using System.Text.Json;

namespace Hw5.SearchIndex.Corpus;

public sealed class CorpusArticleWriter : IAsyncDisposable
{
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions SampleLineOpts = new();

    private readonly string _articlesDir;
    private readonly int _sampleLimit;
    private int _documentCount;
    private int _sampleWritten;
    private StreamWriter? _sampleWriter;

    public CorpusArticleWriter(string processedRoot, int sampleLimit = 100)
    {
        _articlesDir = ProcessedCorpusCatalog.ArticlesDirectory(processedRoot);
        Directory.CreateDirectory(_articlesDir);
        Directory.CreateDirectory(processedRoot);
        _sampleLimit = sampleLimit;
        _sampleWriter = new StreamWriter(
            ProcessedCorpusCatalog.SamplePath(processedRoot),
            append: false,
            System.Text.Encoding.UTF8);
    }

    public int DocumentCount => _documentCount;

    public async Task WriteAsync(WikipediaDocumentRecord record, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_articlesDir, $"{record.Id}.json");
        var json = JsonSerializer.Serialize(record, WriteOpts);
        await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
        _documentCount++;

        if (_sampleWritten < _sampleLimit)
        {
            var line = JsonSerializer.Serialize(record, SampleLineOpts);
            await _sampleWriter!.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
            _sampleWritten++;
        }
    }

    public ProcessedCorpusManifest CompleteManifest() =>
        new()
        {
            Layout = ProcessedCorpusCatalog.LayoutPerArticle,
            ProcessedDocuments = _documentCount,
            ProcessedAtUtc = DateTime.UtcNow.ToString("O"),
            SampleDocuments = _sampleWritten,
            ArticlesDirectory = ProcessedCorpusCatalog.ArticlesDirectoryName,
            SamplePath = ProcessedCorpusCatalog.SampleFileName,
        };

    public async ValueTask DisposeAsync()
    {
        if (_sampleWriter is not null)
        {
            await _sampleWriter.DisposeAsync().ConfigureAwait(false);
            _sampleWriter = null;
        }
    }
}
