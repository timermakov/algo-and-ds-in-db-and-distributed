namespace Hw2.Algorithms.Geo;

public sealed class GeoKdTreeIndex
{
    private readonly Dictionary<string, GeoPoint> _pointsById = new(StringComparer.Ordinal);
    private Node? _root;

    public int Count => _pointsById.Count;

    public void Insert(GeoPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        ArgumentException.ThrowIfNullOrWhiteSpace(point.Id);
        ValidateCoordinate(point.Latitude, point.Longitude);
        if (_pointsById.ContainsKey(point.Id))
        {
            throw new InvalidOperationException($"Point '{point.Id}' already exists.");
        }

        _root = InsertNode(_root, point, depth: 0);
        _pointsById.Add(point.Id, point);
    }

    public IReadOnlyList<GeoSearchResult> SearchRadius(double latitude, double longitude, double radiusMeters)
    {
        ValidateCoordinate(latitude, longitude);
        if (radiusMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMeters), "Radius must be non-negative.");
        }

        if (_root is null)
        {
            return [];
        }

        var results = new List<GeoSearchResult>();
        SearchRadiusCore(_root, latitude, longitude, radiusMeters, results);
        results.Sort(GeoSearchResultComparer.Instance);
        return results;
    }

    public IReadOnlyList<GeoSearchResult> SearchKNearest(double latitude, double longitude, int k)
    {
        ValidateCoordinate(latitude, longitude);
        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k), "K must be positive.");
        }

        if (_root is null)
        {
            return [];
        }

        var best = new List<GeoSearchResult>(k);
        SearchKNearestCore(_root, latitude, longitude, k, best);
        var results = new List<GeoSearchResult>(best);
        results.Sort(GeoSearchResultComparer.Instance);
        return results;
    }

    private static Node InsertNode(Node? node, GeoPoint point, int depth)
    {
        if (node is null)
        {
            return new Node(point, depth);
        }

        var splitByLatitude = depth % 2 == 0;
        var goLeft = splitByLatitude
            ? point.Latitude < node.Point.Latitude
            : point.Longitude < node.Point.Longitude;

        if (goLeft)
        {
            node.Left = InsertNode(node.Left, point, depth + 1);
        }
        else
        {
            node.Right = InsertNode(node.Right, point, depth + 1);
        }

        return node;
    }

    private static void SearchRadiusCore(Node node, double queryLat, double queryLng, double radiusMeters, List<GeoSearchResult> results)
    {
        var distance = GeoDistance.HaversineMeters(queryLat, queryLng, node.Point.Latitude, node.Point.Longitude);
        if (distance <= radiusMeters)
        {
            results.Add(new GeoSearchResult(node.Point, distance));
        }

        var splitByLatitude = node.Depth % 2 == 0;
        var queryAxis = splitByLatitude ? queryLat : queryLng;
        var splitAxis = splitByLatitude ? node.Point.Latitude : node.Point.Longitude;
        var nearBranch = queryAxis < splitAxis ? node.Left : node.Right;
        var farBranch = queryAxis < splitAxis ? node.Right : node.Left;

        if (nearBranch is not null)
        {
            SearchRadiusCore(nearBranch, queryLat, queryLng, radiusMeters, results);
        }

        if (farBranch is null)
        {
            return;
        }

        var axisLowerBound = GeoDistance.AxisLowerBoundMeters(queryLat, queryLng, splitAxis, splitByLatitude);
        if (axisLowerBound <= radiusMeters)
        {
            SearchRadiusCore(farBranch, queryLat, queryLng, radiusMeters, results);
        }
    }

    private static void SearchKNearestCore(
        Node node,
        double queryLat,
        double queryLng,
        int k,
        List<GeoSearchResult> best)
    {
        var distance = GeoDistance.HaversineMeters(queryLat, queryLng, node.Point.Latitude, node.Point.Longitude);
        TryAddBest(best, node.Point, distance, k);

        var splitByLatitude = node.Depth % 2 == 0;
        var queryAxis = splitByLatitude ? queryLat : queryLng;
        var splitAxis = splitByLatitude ? node.Point.Latitude : node.Point.Longitude;
        var nearBranch = queryAxis < splitAxis ? node.Left : node.Right;
        var farBranch = queryAxis < splitAxis ? node.Right : node.Left;

        if (nearBranch is not null)
        {
            SearchKNearestCore(nearBranch, queryLat, queryLng, k, best);
        }

        if (farBranch is null)
        {
            return;
        }

        var axisLowerBound = GeoDistance.AxisLowerBoundMeters(queryLat, queryLng, splitAxis, splitByLatitude);
        if (best.Count < k || axisLowerBound <= CurrentWorstDistance(best))
        {
            SearchKNearestCore(farBranch, queryLat, queryLng, k, best);
        }
    }

    private static void TryAddBest(List<GeoSearchResult> best, GeoPoint point, double distanceMeters, int k)
    {
        var candidate = new GeoSearchResult(point, distanceMeters);
        if (best.Count < k)
        {
            best.Add(candidate);
            best.Sort(GeoSearchResultWorstFirstComparer.Instance);
            return;
        }

        var worst = best[0];
        if (!IsStrictlyBetter(candidate, worst))
        {
            return;
        }

        best[0] = candidate;
        best.Sort(GeoSearchResultWorstFirstComparer.Instance);
    }

    private static bool IsStrictlyBetter(GeoSearchResult candidate, GeoSearchResult currentWorst)
    {
        var byDistance = candidate.DistanceMeters.CompareTo(currentWorst.DistanceMeters);
        if (byDistance != 0)
        {
            return byDistance < 0;
        }

        return StringComparer.Ordinal.Compare(candidate.Point.Id, currentWorst.Point.Id) < 0;
    }

    private static double CurrentWorstDistance(List<GeoSearchResult> best)
    {
        return best.Count == 0 ? double.PositiveInfinity : best[0].DistanceMeters;
    }

    private static void ValidateCoordinate(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be in range [-90, 90].");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be in range [-180, 180].");
        }
    }

    private sealed class Node
    {
        public Node(GeoPoint point, int depth)
        {
            Point = point;
            Depth = depth;
        }

        public GeoPoint Point { get; }

        public int Depth { get; }

        public Node? Left { get; set; }

        public Node? Right { get; set; }
    }
}

internal sealed class GeoSearchResultComparer : IComparer<GeoSearchResult>
{
    public static readonly GeoSearchResultComparer Instance = new();

    public int Compare(GeoSearchResult? x, GeoSearchResult? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        var byDistance = x.DistanceMeters.CompareTo(y.DistanceMeters);
        return byDistance != 0 ? byDistance : StringComparer.Ordinal.Compare(x.Point.Id, y.Point.Id);
    }
}

internal sealed class GeoSearchResultWorstFirstComparer : IComparer<GeoSearchResult>
{
    public static readonly GeoSearchResultWorstFirstComparer Instance = new();

    public int Compare(GeoSearchResult? x, GeoSearchResult? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        var byDistance = y.DistanceMeters.CompareTo(x.DistanceMeters);
        return byDistance != 0 ? byDistance : StringComparer.Ordinal.Compare(y.Point.Id, x.Point.Id);
    }
}
