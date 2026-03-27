namespace Hw2.Algorithms.Geo;

internal static class GeoDistance
{
    private const double EarthRadiusMeters = 6_371_000.0;

    public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var rLat1 = DegreesToRadians(lat1);
        var rLat2 = DegreesToRadians(lat2);

        var sinLat = Math.Sin(dLat / 2);
        var sinLng = Math.Sin(dLng / 2);
        var a = sinLat * sinLat + Math.Cos(rLat1) * Math.Cos(rLat2) * sinLng * sinLng;
        var c = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        return EarthRadiusMeters * c;
    }

    public static double AxisLowerBoundMeters(double queryLat, double queryLng, double splitValue, bool splitByLatitude)
    {
        if (splitByLatitude)
        {
            return HaversineMeters(queryLat, queryLng, splitValue, queryLng);
        }

        return 0.0;
    }

    private static double DegreesToRadians(double value) => value * (Math.PI / 180.0);
}
