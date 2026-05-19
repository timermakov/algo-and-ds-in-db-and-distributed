using BenchmarkDotNet.Attributes;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Ranking;
using Hw5.SearchIndex.Searching;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

public class IndexQueryBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private DiskSegmentIndex _disk = null!;
    private string _segmentPath = string.Empty;
    private readonly QueryExecutor _executor = new();
    private readonly SearchService _search = new();

    [GlobalSetup]
    public void Setup()
    {
        _memory = BenchmarkIndexFactory.BuildMemoryIndex();
        (_disk, _segmentPath) = BenchmarkIndexFactory.BuildDiskIndex(_memory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _disk.Dispose();
        if (File.Exists(_segmentPath))
        {
            File.Delete(_segmentPath);
        }
    }

    [Benchmark(Baseline = true)]
    public int Memory_AndQuery()
    {
        var result = _executor.Execute(_memory, "alpha AND beta");
        return result.Matches.DocumentIds.Count;
    }

    [Benchmark]
    public int DiskMmap_AndQuery()
    {
        var result = _executor.Execute(_disk, "alpha AND beta");
        return result.Matches.DocumentIds.Count;
    }

    [Benchmark]
    public int Memory_NearAdjQuery()
    {
        var result = _executor.Execute(_memory, "alpha NEAR/3 beta OR gamma ADJ delta");
        return result.Matches.DocumentIds.Count;
    }

    [Benchmark]
    public double Memory_Bm25Top10()
    {
        var ranked = _search.Search(_memory, "alpha OR beta", topK: 10, rankingMode: RankingMode.Bm25);
        return ranked.Sum(static x => x.Score);
    }
}
