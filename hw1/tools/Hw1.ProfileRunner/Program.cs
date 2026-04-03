using System.Diagnostics;
using Hw1.Algorithms.FileHashing;
using Hw1.Algorithms.Lsh;
using Hw1.Algorithms.PerfectHashing;

var argsMap = ParseArgs(args);
var mode = GetString(argsMap, "mode", "filehash");
var seconds = GetInt(argsMap, "seconds", 30);
var n = GetInt(argsMap, "n", 10_000);

if (seconds <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(seconds), "seconds must be positive.");
}

if (n <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(n), "n must be positive.");
}

Console.WriteLine($"Mode={mode} Seconds={seconds} N={n}");
var checksum = mode.ToLowerInvariant() switch
{
    "filehash" => RunFileHash(seconds, n),
    "perfecthash" => RunPerfectHash(seconds, n),
    "lsh" => RunLsh(seconds, n),
    _ => throw new ArgumentException("mode must be filehash|perfecthash|lsh", nameof(mode)),
};

Console.WriteLine($"Checksum={checksum}");

static long RunFileHash(int seconds, int n)
{
    var path = Path.Combine(Path.GetTempPath(), $"hw1-profile-filehash-{Guid.NewGuid():N}.bin");
    var bucketCount = NextPow2(Math.Max(64, n * 4));
    using var table = FileBucketHashTable.Open(path, new FileBucketHashOptions(bucketCount, 16, CreateNew: true));
    try
    {
        var keys = Enumerable.Range(1, n).Select(i => (ulong)i).ToArray();
        foreach (var key in keys)
        {
            table.Insert(key, (long)key);
        }

        var sw = Stopwatch.StartNew();
        var checksum = 0L;
        var cursor = 0;
        while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            var key = keys[cursor++ % keys.Length];
            table.Update(key, (long)key + cursor);
            if (table.TryGet(key, out var value))
            {
                checksum += value & 1023;
            }
        }

        return checksum;
    }
    finally
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

static long RunPerfectHash(int seconds, int n)
{
    var data = Enumerable.Range(0, n)
        .Select(i => new KeyValuePair<string, long>($"key-{i}", i))
        .ToArray();
    var index = StaticPerfectHashIndex.Build(data);
    var keys = data.Select(x => x.Key).ToArray();

    var sw = Stopwatch.StartNew();
    var checksum = 0L;
    var cursor = 0;
    while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
    {
        var key = keys[cursor++ % keys.Length];
        if (index.TryGet(key, out var value))
        {
            checksum += value & 1023;
        }
    }

    return checksum;
}

static long RunLsh(int seconds, int n)
{
    var index = new TextLshIndex(new LshConfig(NumHashes: 64, Bands: 8, ShingleSize: 2, SimilarityThreshold: 0.75));
    var docs = Enumerable.Range(0, n)
        .Select(i => new TextDocument($"doc-{i}", $"distributed database index shard quorum latency {i % 257}"))
        .ToArray();
    index.BuildIndex(docs);
    var queries = docs.Take(Math.Min(1024, docs.Length)).Select(x => x.Text).ToArray();

    var sw = Stopwatch.StartNew();
    var checksum = 0L;
    var cursor = 0;
    while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
    {
        var query = queries[cursor++ % queries.Length];
        checksum += index.FindDuplicatesLsh(query, threshold: 0.75).Count;
    }

    return checksum;
}

static int NextPow2(int n)
{
    var value = 1;
    while (value < n)
    {
        value <<= 1;
    }

    return value;
}

static Dictionary<string, string> ParseArgs(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var token = args[i];
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        var key = token[2..];
        var value = i + 1 < args.Length ? args[i + 1] : string.Empty;
        if (value.StartsWith("--", StringComparison.Ordinal))
        {
            value = "true";
        }
        else
        {
            i++;
        }

        result[key] = value;
    }

    return result;
}

static string GetString(Dictionary<string, string> argsMap, string key, string fallback)
{
    return argsMap.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
}

static int GetInt(Dictionary<string, string> argsMap, string key, int fallback)
{
    if (!argsMap.TryGetValue(key, out var value))
    {
        return fallback;
    }

    return int.TryParse(value, out var parsed) ? parsed : fallback;
}
