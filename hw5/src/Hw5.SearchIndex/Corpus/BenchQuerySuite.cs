using System.Text.Json;

namespace Hw5.SearchIndex.Corpus;

public sealed record BenchTermPair(string Tier, string TermA, string TermB, string ExcludeTerm);

public sealed class BenchQuerySuite
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public IReadOnlyList<BenchTermPair> Pairs { get; init; } = [];

    public IReadOnlyList<string> And { get; init; } = [];

    public IReadOnlyList<string> Or { get; init; } = [];

    public IReadOnlyList<string> Not { get; init; } = [];

    public IReadOnlyList<string> Adj { get; init; } = [];

    public IReadOnlyList<string> Near { get; init; } = [];

    public IReadOnlyList<string> Composite { get; init; } = [];

    public string PrimaryAnd => And.Count > 0 ? And[0] : "alpha AND beta";

    public string PrimaryOr => Or.Count > 0 ? Or[0] : "alpha OR beta";

    public string PrimaryNot => Not.Count > 0 ? Not[0] : "alpha AND NOT delta";

    public string PrimaryNear => Near.Count > 0 ? Near[0] : "alpha NEAR/3 beta";

    public IReadOnlyList<string> LegacyFlatList()
    {
        var list = new List<string>(And.Count + Or.Count + Not.Count + Adj.Count + Near.Count + Composite.Count);
        list.AddRange(And);
        list.AddRange(Or);
        list.AddRange(Not);
        list.AddRange(Adj);
        list.AddRange(Near);
        list.AddRange(Composite);
        return list;
    }

    public IReadOnlyList<string> ToLegacySlots() =>
    [
        PrimaryAnd,
        PrimaryOr,
        PrimaryNot,
        Adj.Count > 0 ? Adj[0] : PrimaryAnd,
        PrimaryNear,
        .. Composite,
    ];

    public static BenchQuerySuite BuildSynthetic()
    {
        var pairs = new[]
        {
            new BenchTermPair("high", "alpha", "beta", "delta"),
            new BenchTermPair("mid", "gamma", "delta", "alpha"),
            new BenchTermPair("low", "kappa", "lambda", "beta"),
        };

        return FromPairs(pairs);
    }

    public static BenchQuerySuite FromPairs(IReadOnlyList<BenchTermPair> pairs)
    {
        var and = new List<string>(pairs.Count);
        var or = new List<string>(pairs.Count);
        var not = new List<string>(pairs.Count);
        var adj = new List<string>(pairs.Count);
        var near = new List<string>(pairs.Count);

        foreach (var pair in pairs)
        {
            and.Add($"{pair.TermA} AND {pair.TermB}");
            or.Add($"{pair.TermA} OR {pair.TermB}");
            not.Add($"{pair.TermA} AND NOT {pair.ExcludeTerm}");
            adj.Add($"{pair.TermA} ADJ {pair.TermB}");
            near.Add($"{pair.TermA} NEAR/{WikiBenchQuerySelector.NearDistance} {pair.TermB}");
        }

        var first = pairs[0];
        var composite = new List<string>
        {
            $"({first.TermA} AND {first.TermB}) OR {first.ExcludeTerm}",
            $"{pairs[^1].TermA} NEAR/2 {pairs[^1].TermB} AND NOT {pairs[^1].ExcludeTerm}",
        };

        return new BenchQuerySuite
        {
            Pairs = pairs,
            And = and,
            Or = or,
            Not = not,
            Adj = adj,
            Near = near,
            Composite = composite,
        };
    }

    public static void SaveJson(string path, BenchQuerySuite suite, int documentCount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var payload = new BenchQuerySuiteDto
        {
            DocumentCount = documentCount,
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            Pairs = suite.Pairs.Select(static p => new BenchTermPairDto
            {
                Tier = p.Tier,
                TermA = p.TermA,
                TermB = p.TermB,
                ExcludeTerm = p.ExcludeTerm,
            }).ToList(),
            And = suite.And.ToList(),
            Or = suite.Or.ToList(),
            Not = suite.Not.ToList(),
            Adj = suite.Adj.ToList(),
            Near = suite.Near.ToList(),
            Composite = suite.Composite.ToList(),
        };
        File.WriteAllText(path, JsonSerializer.Serialize(payload, JsonOpts));
    }

    public static BenchQuerySuite LoadJson(string path)
    {
        if (!File.Exists(path))
        {
            return BuildSynthetic();
        }

        var dto = JsonSerializer.Deserialize<BenchQuerySuiteDto>(File.ReadAllText(path), JsonOpts)
            ?? throw new InvalidDataException($"Invalid query suite: {path}");

        var pairs = dto.Pairs.Select(static p => new BenchTermPair(p.Tier, p.TermA, p.TermB, p.ExcludeTerm)).ToList();
        if (pairs.Count > 0 && dto.And.Count > 0)
        {
            return new BenchQuerySuite
            {
                Pairs = pairs,
                And = dto.And,
                Or = dto.Or,
                Not = dto.Not,
                Adj = dto.Adj,
                Near = dto.Near,
                Composite = dto.Composite,
            };
        }

        return FromPairs(pairs);
    }

    private sealed class BenchQuerySuiteDto
    {
        public int DocumentCount { get; set; }
        public string GeneratedAtUtc { get; set; } = string.Empty;
        public List<BenchTermPairDto> Pairs { get; set; } = [];
        public List<string> And { get; set; } = [];
        public List<string> Or { get; set; } = [];
        public List<string> Not { get; set; } = [];
        public List<string> Adj { get; set; } = [];
        public List<string> Near { get; set; } = [];
        public List<string> Composite { get; set; } = [];
    }

    private sealed class BenchTermPairDto
    {
        public string Tier { get; set; } = string.Empty;
        public string TermA { get; set; } = string.Empty;
        public string TermB { get; set; } = string.Empty;
        public string ExcludeTerm { get; set; } = string.Empty;
    }
}
