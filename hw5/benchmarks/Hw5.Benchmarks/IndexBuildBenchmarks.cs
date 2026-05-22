using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class IndexBuildBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private string _segmentPath = string.Empty;

    [ParamsSource(nameof(AllCases))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> AllCases() => BenchCorpusCase.MainCases();

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        _segmentPath = Path.Combine(Path.GetTempPath(), $"hw5-build-{Guid.NewGuid():N}.seg");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_segmentPath))
        {
            File.Delete(_segmentPath);
        }
    }

    [Benchmark]
    public long SealAndWriteSegment()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-build-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, _memory);
            var bytes = new FileInfo(path).Length;
            AppendCompressionStat(Case, bytes);
            return bytes;
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Benchmark]
    public long NaivePostingBytes()
    {
        long total = 0;
        foreach (var term in _memory.Terms)
        {
            var postings = _memory.GetPostings(term);
            for (var i = 0; i < postings.Count; i++)
            {
                total += sizeof(int);
                total += postings.PositionsAt(i).Count * sizeof(int);
            }
        }

        return total;
    }

    private static void AppendCompressionStat(BenchCorpusCase benchCase, long segmentBytes)
    {
        var outDir = Path.Combine(BenchRuntime.Hw5Root, "reports", "artifacts");
        Directory.CreateDirectory(outDir);
        var path = Path.Combine(outDir, "compression_by_corpus.json");
        List<CompressionRow> rows;
        if (File.Exists(path))
        {
            rows = JsonSerializer.Deserialize<List<CompressionRow>>(File.ReadAllText(path)) ?? [];
        }
        else
        {
            rows = [];
        }

        rows.RemoveAll(r => r.Corpus == benchCase.Corpus.ToString() && r.DocumentCount == benchCase.DocumentCount);
        rows.Add(new CompressionRow
        {
            Corpus = benchCase.Corpus.ToString(),
            DocumentCount = benchCase.DocumentCount,
            SegmentFileBytes = segmentBytes,
        });
        File.WriteAllText(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class CompressionRow
    {
        public string Corpus { get; set; } = string.Empty;
        public int DocumentCount { get; set; }
        public long SegmentFileBytes { get; set; }
    }
}
