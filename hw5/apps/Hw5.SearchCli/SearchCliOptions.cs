namespace Hw5.SearchCli;

public sealed class SearchCliOptions
{
    public const int DefaultWikiDocuments = 200;

    public bool LoadWiki { get; init; }

    public int MaxDocuments { get; init; } = DefaultWikiDocuments;

    public string WikiJsonlPath { get; init; } = "data/processed";

    public static SearchCliOptions Parse(string[] args)
    {
        var loadWiki = HasCorpusWiki(args) || string.Equals(
            Environment.GetEnvironmentVariable("HW5_CORPUS"),
            "wiki",
            StringComparison.OrdinalIgnoreCase);

        var maxDocuments = ParseIntArg(args, "--max-docs")
            ?? ParseIntArg(args, "--docs")
            ?? ParseIntFromEnv("HW5_WIKI_DOCS")
            ?? ParseIntFromEnv("HW5_MAX_DOCS")
            ?? DefaultWikiDocuments;

        if (maxDocuments <= 0)
        {
            throw new ArgumentException("Число документов должно быть положительным (--max-docs).");
        }

        var wikiPath = GetArg(args, "--wiki-path")
            ?? Environment.GetEnvironmentVariable("HW5_WIKI_PATH")
            ?? "data/processed";

        return new SearchCliOptions
        {
            LoadWiki = loadWiki,
            MaxDocuments = maxDocuments,
            WikiJsonlPath = wikiPath,
        };
    }

    private static bool HasCorpusWiki(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--corpus", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
                && string.Equals(args[i + 1], "wiki", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static int? ParseIntArg(string[] args, string name)
    {
        var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || idx + 1 >= args.Length || !int.TryParse(args[idx + 1], out var value))
        {
            return null;
        }

        return value;
    }

    private static int? ParseIntFromEnv(string name)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var value) ? value : null;
    }

    private static string? GetArg(string[] args, string name)
    {
        var idx = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        return idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : null;
    }
}
