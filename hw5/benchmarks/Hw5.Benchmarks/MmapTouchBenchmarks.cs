using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Hw5.SearchIndex.Corpus;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Querying;
using Hw5.SearchIndex.Storage;

namespace Hw5.Benchmarks;

[Config(typeof(WarmOnlyBenchmarkConfig))]
public class MmapTouchBenchmarks
{
    private InMemoryPositionalIndex _memory = null!;
    private string _query = string.Empty;
    private readonly QueryExecutor _executor = new();

    [ParamsSource(nameof(MainSyntheticCase))]
    public BenchCorpusCase Case { get; set; }

    public static IEnumerable<BenchCorpusCase> MainSyntheticCase()
    {
        yield return new BenchCorpusCase
        {
            Corpus = CorpusKind.Synthetic,
            DocumentCount = BenchRuntime.Current.DocumentCounts[0],
        };
    }

    [GlobalSetup]
    public void Setup()
    {
        _memory = CorpusBenchmarkBuilder.BuildMemoryIndex(Case.Corpus, Case.DocumentCount);
        _query = CorpusBenchmarkBuilder.LoadQueries(Case.Corpus)[0];
    }

    [Benchmark(Baseline = true)]
    public int FirstTouchMmap_AndQuery()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-mmap-first-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, _memory);
            using var disk = new DiskSegmentIndex(path);
            return _executor.Execute(disk, _query).Matches.DocumentIds.Count;
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Benchmark(OperationsPerInvoke = BenchRuntime.DefaultOperationsPerInvoke)]
    public int RepeatedMmap_AndQuery()
    {
        var path = Path.Combine(Path.GetTempPath(), $"hw5-mmap-repeat-{Guid.NewGuid():N}.seg");
        try
        {
            SegmentSerializer.Write(path, _memory);
            using var disk = new DiskSegmentIndex(path);
            var total = 0;
            for (var i = 0; i < BenchRuntime.Current.OperationsPerInvoke; i++)
            {
                total += _executor.Execute(disk, _query).Matches.DocumentIds.Count;
            }

            return total;
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
