using FsCheck.Xunit;
using Hw2.Algorithms.Geo;

namespace Hw2.Algorithms.Tests;

public sealed class GeoKdTreeIndexPropertyTests
{
    [Property(MaxTest = 40)]
    public bool RadiusQueryMatchesFullScan(int[] rawValues)
    {
        var points = BuildPoints(rawValues, maxCount: 300);
        if (points.Count == 0)
        {
            return true;
        }

        var index = BuildIndex(points);
        var query = points[points.Count / 2];
        var radiusMeters = 5_000 + ((points.Count * 97) % 150_000);

        var actual = index.SearchRadius(query.Latitude, query.Longitude, radiusMeters);
        var expected = FullScanRadius(points, query.Latitude, query.Longitude, radiusMeters);
        return AreEquivalent(expected, actual);
    }

    [Property(MaxTest = 40)]
    public bool KnnQueryMatchesFullScan(int[] rawValues)
    {
        var points = BuildPoints(rawValues, maxCount: 350);
        if (points.Count == 0)
        {
            return true;
        }

        var index = BuildIndex(points);
        var query = points[points.Count / 3];
        var k = Math.Clamp(points.Count / 20 + 1, 1, Math.Min(50, points.Count));

        var actual = index.SearchKNearest(query.Latitude, query.Longitude, k);
        var expected = FullScanKnn(points, query.Latitude, query.Longitude, k);
        return AreEquivalent(expected, actual);
    }

    [Fact]
    public void InsertDuplicateIdThrows()
    {
        var index = new GeoKdTreeIndex();
        index.Insert(new GeoPoint("dup", 55.7558, 37.6176));

        var action = () => index.Insert(new GeoPoint("dup", 59.9343, 30.3351));

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void EmptyIndexReturnsEmptyResults()
    {
        var index = new GeoKdTreeIndex();

        Assert.Empty(index.SearchRadius(55.7558, 37.6176, 1_000));
        Assert.Empty(index.SearchKNearest(55.7558, 37.6176, 10));
    }

    private static GeoKdTreeIndex BuildIndex(IReadOnlyList<GeoPoint> points)
    {
        var index = new GeoKdTreeIndex();
        foreach (var point in points)
        {
            index.Insert(point);
        }

        return index;
    }

    private static List<GeoPoint> BuildPoints(int[] rawValues, int maxCount)
    {
        var limit = Math.Min(maxCount, rawValues.Length / 2);
        var points = new List<GeoPoint>(limit);
        for (var i = 0; i < limit; i++)
        {
            var latRaw = rawValues[i * 2];
            var lngRaw = rawValues[i * 2 + 1];
            var lat = Normalize(latRaw, -89.9, 89.9);
            var lng = Normalize(lngRaw, -179.9, 179.9);
            points.Add(new GeoPoint($"p-{i}", lat, lng));
        }

        return points;
    }

    private static IReadOnlyList<GeoSearchResult> FullScanRadius(
        IReadOnlyList<GeoPoint> points,
        double queryLat,
        double queryLng,
        double radiusMeters)
    {
        return points
            .Select(point => new GeoSearchResult(point, HaversineMeters(queryLat, queryLng, point.Latitude, point.Longitude)))
            .Where(result => result.DistanceMeters <= radiusMeters)
            .OrderBy(result => result.DistanceMeters)
            .ThenBy(result => result.Point.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<GeoSearchResult> FullScanKnn(
        IReadOnlyList<GeoPoint> points,
        double queryLat,
        double queryLng,
        int k)
    {
        return points
            .Select(point => new GeoSearchResult(point, HaversineMeters(queryLat, queryLng, point.Latitude, point.Longitude)))
            .OrderBy(result => result.DistanceMeters)
            .ThenBy(result => result.Point.Id, StringComparer.Ordinal)
            .Take(k)
            .ToArray();
    }

    private static bool AreEquivalent(IReadOnlyList<GeoSearchResult> expected, IReadOnlyList<GeoSearchResult> actual)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (var i = 0; i < expected.Count; i++)
        {
            var e = expected[i];
            var a = actual[i];
            if (!string.Equals(e.Point.Id, a.Point.Id, StringComparison.Ordinal))
            {
                return false;
            }

            if (Math.Abs(e.DistanceMeters - a.DistanceMeters) > 1e-6)
            {
                return false;
            }
        }

        return true;
    }

    private static double Normalize(int value, double min, double max)
    {
        var unit = (value - (double)int.MinValue) / ((double)uint.MaxValue);
        return min + (max - min) * unit;
    }

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusMeters = 6_371_000.0;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);

        var sinLat = Math.Sin(dLat / 2);
        var sinLng = Math.Sin(dLng / 2);
        var a = sinLat * sinLat + Math.Cos(rLat1) * Math.Cos(rLat2) * sinLng * sinLng;
        var c = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double value) => value * (Math.PI / 180.0);
}
