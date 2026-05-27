using System.Globalization;
using Microsoft.Coyote;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Xunit;

namespace Hw4.ConcurrentMap.Tests;

/// <summary>
/// Systematic concurrency exploration via Coyote <see cref="TestingEngine"/>.
/// Для полного перехвата Task-планирования нужен шаг <c>coyote rewrite</c> (см. README).
/// Глубина перебора — <see cref="CoyoteIterationCount"/> (переменная <c>HW4_COYOTE_ITERATIONS</c>).
/// </summary>
[Trait("Category", "Coyote")]
public sealed class CoyoteConcurrencyTests
{
    /// <summary>Низкий дефолт — Coyote занимает большую часть времени <c>dotnet test</c>.</summary>
    internal static uint CoyoteIterationCount
    {
        get
        {
            var raw = Environment.GetEnvironmentVariable("HW4_COYOTE_ITERATIONS");
            if (raw is null || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return 10;
            var clamped = Math.Clamp(n, 1, 50_000);
            return (uint)clamped;
        }
    }

    [Fact]
    public void Engine_explores_parallel_puts_distinct_keys()
    {
        var config = Configuration.Create()
            .WithTestingIterations(CoyoteIterationCount);

        using var engine = TestingEngine.Create(config, ParallelPutsDistinctKeys);
        engine.Run();

        Assert.True(engine.TestReport.NumOfFoundBugs == 0,
            $"Coyote reports {engine.TestReport.NumOfFoundBugs} bug(s); trace: {engine.ReproducibleTrace}");
    }

    [Fact]
    public void Engine_explores_parallel_merge_distinct_keys()
    {
        var config = Configuration.Create()
            .WithTestingIterations(CoyoteIterationCount);

        using var engine = TestingEngine.Create(config, ParallelMergeDistinctKeys);
        engine.Run();

        Assert.True(engine.TestReport.NumOfFoundBugs == 0,
            $"Coyote reports {engine.TestReport.NumOfFoundBugs} bug(s); trace: {engine.ReproducibleTrace}");
    }

    static void ParallelPutsDistinctKeys()
    {
        using var map = new ConcurrentHashTable<int, string>(initialCapacity: 512);

        var a = Task.Run(() => map.Put(101, "a"));
        var b = Task.Run(() => map.Put(202, "b"));
        Task.WaitAll(a, b);

        Specification.Assert(map.Get(101) == "a", "key 101 mapping missing or wrong");
        Specification.Assert(map.Get(202) == "b", "key 202 mapping missing or wrong");
    }

    static void ParallelMergeDistinctKeys()
    {
        using var map = new ConcurrentHashTable<string, string>(initialCapacity: 256);

        var t1 = Task.Run(() => map.Merge("x", "a", (u, v) => u + v));
        var t2 = Task.Run(() => map.Merge("y", "b", (u, v) => u + v));
        Task.WaitAll(t1, t2);

        Specification.Assert(map.Get("x") == "a", "merge path key x wrong");
        Specification.Assert(map.Get("y") == "b", "merge path key y wrong");
    }
}
