using System.Diagnostics;
using Hw2.Algorithms.Geo;

var argsMap = ParseArgs(args);
var mode = GetString(argsMap, "mode", "radius");
var seconds = GetInt(argsMap, "seconds", 120);
var n = GetInt(argsMap, "n", 100_000);
var radiusMeters = GetInt(argsMap, "radius", 1_000);
var k = GetInt(argsMap, "k", 20);

if (seconds <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(seconds), "seconds must be positive.");
}

if (n <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(n), "n must be positive.");
}

if (radiusMeters <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(radiusMeters), "radius must be positive.");
}

if (k <= 0)
{
    throw new ArgumentOutOfRangeException(nameof(k), "k must be positive.");
}

Console.WriteLine($"Mode={mode} Seconds={seconds} N={n} Radius={radiusMeters} K={k}");
var points = BuildPoints(n);
var index = new GeoKdTreeIndex();
foreach (var point in points)
{
    index.Insert(point);
}

var queries = points.Take(Math.Min(2_048, points.Count)).ToArray();
if (queries.Length == 0)
{
    throw new InvalidOperationException("No queries generated.");
}

var sw = Stopwatch.StartNew();
var opCount = 0L;
var checksum = 0L;
var q = 0;
while (sw.Elapsed < TimeSpan.FromSeconds(seconds))
{
    var point = queries[q++ % queries.Length];
    if (string.Equals(mode, "radius", StringComparison.OrdinalIgnoreCase))
    {
        var matches = index.SearchRadius(point.Latitude, point.Longitude, radiusMeters);
        checksum += matches.Count;
    }
    else if (string.Equals(mode, "knn", StringComparison.OrdinalIgnoreCase))
    {
        var nearest = index.SearchKNearest(point.Latitude, point.Longitude, k);
        checksum += nearest.Count == 0 ? 0 : (long)Math.Round(nearest[0].DistanceMeters);
    }
    else
    {
        throw new ArgumentException("mode must be 'radius' or 'knn'.", nameof(mode));
    }

    opCount++;
}

sw.Stop();
Console.WriteLine($"Ops={opCount} ElapsedMs={sw.Elapsed.TotalMilliseconds:F2} ThroughputOpsPerSec={opCount / sw.Elapsed.TotalSeconds:F2}");
Console.WriteLine($"Checksum={checksum}");

static List<GeoPoint> BuildPoints(int count)
{
    var points = new List<GeoPoint>(count);
    for (var i = 0; i < count; i++)
    {
        var lat = -89.0 + (178.0 * (i % 4_096) / 4_095.0);
        var lng = -179.0 + (358.0 * ((i * 17) % 8_192) / 8_191.0);
        points.Add(new GeoPoint($"p-{i}", lat, lng));
    }

    return points;
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
