using System.Diagnostics;
using Hw2.Algorithms.Geo;

namespace Hw2.Algorithms.Tests;

public sealed class GeoKdTreeIndexPerfTests
{
    [Fact]
    public void RadiusQueryThroughputOnLargeDataset()
    {
        const int n = 100_000;
        const int operations = 10_000;
        const double radiusMeters = 2_000;

        var points = GeneratePoints(n);
        var index = BuildIndex(points);

        var sw = Stopwatch.StartNew();
        var hits = 0;
        for (var i = 0; i < operations; i++)
        {
            var point = points[i % points.Count];
            hits += index.SearchRadius(point.Latitude, point.Longitude, radiusMeters).Count;
        }

        sw.Stop();
        var throughput = operations / sw.Elapsed.TotalSeconds;

        Assert.True(hits >= 0);
        Assert.True(throughput > 0);
    }

    [Fact]
    public void KnnQueryLatencyOnLargeDataset()
    {
        const int n = 100_000;
        const int operations = 8_000;
        const int k = 10;

        var points = GeneratePoints(n);
        var index = BuildIndex(points);

        var sw = Stopwatch.StartNew();
        var distanceSum = 0.0;
        for (var i = 0; i < operations; i++)
        {
            var point = points[(i * 17) % points.Count];
            var result = index.SearchKNearest(point.Latitude, point.Longitude, k);
            distanceSum += result[0].DistanceMeters;
        }

        sw.Stop();
        var avgLatencyUs = sw.Elapsed.TotalMilliseconds * 1000.0 / operations;

        Assert.True(distanceSum >= 0);
        Assert.True(avgLatencyUs > 0);
    }

    private static GeoKdTreeIndex BuildIndex(IEnumerable<GeoPoint> points)
    {
        var index = new GeoKdTreeIndex();
        foreach (var point in points)
        {
            index.Insert(point);
        }

        return index;
    }

    private static List<GeoPoint> GeneratePoints(int count)
    {
        var points = new List<GeoPoint>(count);
        for (var i = 0; i < count; i++)
        {
            var lat = -89.0 + (178.0 * (i % 4_096) / 4_095.0);
            var lng = -179.0 + (358.0 * ((i * 13) % 8_192) / 8_191.0);
            points.Add(new GeoPoint($"geo-{i}", lat, lng));
        }

        return points;
    }
}
