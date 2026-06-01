using System.Text.Json;



namespace Hw5.SearchIndex.Corpus;



public static partial class ProcessedCorpusCatalog

{

    public const string LayoutPerArticle = "per-article";



    public const string ManifestFileName = "corpus.manifest.json";

    public const string ArticlesDirectoryName = "articles";

    public const string SampleFileName = "docs-sample.jsonl";

    public const string DocsJsonlFileName = "docs.jsonl";

}



public sealed record ProcessedCorpusManifest

{

    public string Layout { get; init; } = ProcessedCorpusCatalog.LayoutPerArticle;

    public int ProcessedDocuments { get; init; }

    public string ProcessedAtUtc { get; init; } = string.Empty;

    public int SampleDocuments { get; init; }

    public string ArticlesDirectory { get; init; } = ProcessedCorpusCatalog.ArticlesDirectoryName;

    public string SamplePath { get; init; } = ProcessedCorpusCatalog.SampleFileName;

}



public static partial class ProcessedCorpusCatalog

{

    private static readonly JsonSerializerOptions JsonOpts = new()

    {

        PropertyNameCaseInsensitive = true,

        WriteIndented = true,

    };



    public static string ManifestPath(string processedRoot) =>

        Path.Combine(processedRoot, ManifestFileName);



    public static string ArticlesDirectory(string processedRoot) =>

        Path.Combine(processedRoot, ArticlesDirectoryName);



    public static string SamplePath(string processedRoot) =>

        Path.Combine(processedRoot, SampleFileName);



    public static string DocsJsonlPath(string processedRoot) =>

        Path.Combine(processedRoot, DocsJsonlFileName);



    public static string ArticlePath(string processedRoot, int pageId) =>

        Path.Combine(ArticlesDirectory(processedRoot), $"{pageId}.json");



    public static bool IsCorpusAvailable(string processedRootOrFile)

    {

        if (File.Exists(processedRootOrFile))

        {

            return true;

        }



        if (!Directory.Exists(processedRootOrFile))

        {

            return false;

        }



        if (HasArticleFiles(processedRootOrFile))

        {

            return true;

        }



        return File.Exists(DocsJsonlPath(processedRootOrFile));

    }



    public static bool HasArticleFiles(string processedRoot)

    {

        var dir = ArticlesDirectory(processedRoot);

        return Directory.Exists(dir)

            && Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).Any();

    }



    public static string ResolveProcessedRoot(string pathOrRoot, string hw5Root)

    {

        if (File.Exists(pathOrRoot))

        {

            return Path.GetDirectoryName(pathOrRoot)!;

        }



        if (Directory.Exists(pathOrRoot))

        {

            return pathOrRoot;

        }



        var candidate = Path.Combine(hw5Root, pathOrRoot);

        if (File.Exists(candidate))

        {

            return Path.GetDirectoryName(candidate)!;

        }



        if (Directory.Exists(candidate))

        {

            return candidate;

        }



        return Path.Combine(hw5Root, "data", "processed");

    }



    public static ProcessedCorpusManifest? TryReadManifest(string processedRoot)

    {

        var manifestPath = ManifestPath(processedRoot);

        if (!File.Exists(manifestPath))

        {

            return null;

        }



        return JsonSerializer.Deserialize<ProcessedCorpusManifest>(File.ReadAllText(manifestPath), JsonOpts);

    }



    public static IReadOnlyList<string> ResolveArticleFiles(string processedRoot)

    {

        var manifest = TryReadManifest(processedRoot);

        if (manifest is not null

            && string.Equals(manifest.Layout, LayoutPerArticle, StringComparison.OrdinalIgnoreCase)

            && HasArticleFiles(processedRoot))

        {

            return EnumerateArticleFiles(ArticlesDirectory(processedRoot));

        }



        if (HasArticleFiles(processedRoot))

        {

            return EnumerateArticleFiles(ArticlesDirectory(processedRoot));

        }



        return [];

    }



    public static void WriteManifest(string processedRoot, ProcessedCorpusManifest manifest)

    {

        Directory.CreateDirectory(processedRoot);

        File.WriteAllText(ManifestPath(processedRoot), JsonSerializer.Serialize(manifest, JsonOpts));

    }



    private static List<string> EnumerateArticleFiles(string articlesDir) =>

        Directory

            .EnumerateFiles(articlesDir, "*.json", SearchOption.TopDirectoryOnly)

            .OrderBy(static path => ParseArticleId(path))

            .ToList();



    private static int ParseArticleId(string path)

    {

        var name = Path.GetFileNameWithoutExtension(path);

        return int.TryParse(name, out var id) ? id : int.MaxValue;

    }

}


