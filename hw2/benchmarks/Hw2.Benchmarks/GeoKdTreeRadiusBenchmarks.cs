using BenchmarkDotNet.Attributes;
using Hw2.Algorithms.Geo;

namespace Hw2.Benchmarks;

[Config(typeof(StableBenchmarkConfig))]
public class GeoKdTreeRadiusBenchmarks
{
    private const int KdTreeOperationsPerInvoke = 128;
    private const int FullScanOperationsPerInvoke = 32;

    [Params(10_000, 21_544, 35_938)]
    public int N;

    [Params(100, 5_000)]
    public int RadiusMeters;

    private GeoKdTreeIndex _index = null!;
    private GeoPoint[] _points = [];
    private GeoPoint[] _queries = [];
    private int _cursor;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _points = BuildPoints(N).ToArray();
        _queries = _points.Take(Math.Min(_points.Length, 500)).ToArray();
        _index = new GeoKdTreeIndex();
        foreach (var point in _points)
        {
            _index.Insert(point);
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _cursor = 0;
    }

    [Benchmark(OperationsPerInvoke = KdTreeOperationsPerInvoke)]
    public int QueryKdTreeRadius()
    {
        var total = 0;
        for (var i = 0; i < KdTreeOperationsPerInvoke; i++)
        {
            var query = _queries[_cursor++ % _queries.Length];
            total += _index.SearchRadius(query.Latitude, query.Longitude, RadiusMeters).Count;
        }

        return total;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = FullScanOperationsPerInvoke)]
    public int QueryFullScanRadius()
    {
        var total = 0;
        for (var i = 0; i < FullScanOperationsPerInvoke; i++)
        {
            var query = _queries[_cursor++ % _queries.Length];
            total += FullScanRadius(query.Latitude, query.Longitude, RadiusMeters);
        }

        return total;
    }

    private int FullScanRadius(double lat, double lng, double radiusMeters)
    {
        var hits = 0;
        for (var i = 0; i < _points.Length; i++)
        {
            var point = _points[i];
            if (HaversineMeters(lat, lng, point.Latitude, point.Longitude) <= radiusMeters)
            {
                hits++;
            }
        }

        return hits;
    }

    private static IEnumerable<GeoPoint> BuildPoints(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var lat = -89.0 + (178.0 * (i % 4_096) / 4_095.0);
            var lng = -179.0 + (358.0 * ((i * 17) % 8_192) / 8_191.0);
            yield return new GeoPoint($"p-{i}", lat, lng);
        }
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
