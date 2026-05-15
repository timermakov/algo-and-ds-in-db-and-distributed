using Hw5.SearchIndex.Indexing;

namespace Hw5.SearchIndex.Querying;

public sealed class MatchSet
{
    private readonly Dictionary<int, IReadOnlyList<int>> _positionsByDocument;

    public MatchSet(Dictionary<int, IReadOnlyList<int>> positionsByDocument)
    {
        _positionsByDocument = positionsByDocument;
    }

    public static MatchSet Empty { get; } = new(new Dictionary<int, IReadOnlyList<int>>());

    public IReadOnlyCollection<int> DocumentIds => _positionsByDocument.Keys;

    public IReadOnlyDictionary<int, IReadOnlyList<int>> PositionsByDocument => _positionsByDocument;

    public IReadOnlyList<int> SortedDocumentIds() => [.. _positionsByDocument.Keys.OrderBy(static id => id)];

    public static MatchSet FromPostingList(PostingList postingList)
    {
        var map = new Dictionary<int, IReadOnlyList<int>>();
        for (var i = 0; i < postingList.Count; i++)
        {
            map[postingList.DocumentIdAt(i)] = postingList.PositionsAt(i);
        }

        return new MatchSet(map);
    }

    public MatchSet And(MatchSet other)
    {
        var docs = PostingListOperators.Intersect(ToDocPostingList(), other.ToDocPostingList());
        var map = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var docId in docs)
        {
            var merged = _positionsByDocument[docId]
                .Concat(other._positionsByDocument[docId])
                .Distinct()
                .Order()
                .ToArray();
            map[docId] = merged;
        }

        return new MatchSet(map);
    }

    public MatchSet Or(MatchSet other)
    {
        var docs = PostingListOperators.Union(ToDocPostingList(), other.ToDocPostingList());
        var map = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var docId in docs)
        {
            var hasLeft = _positionsByDocument.TryGetValue(docId, out var left);
            var hasRight = other._positionsByDocument.TryGetValue(docId, out var right);
            if (hasLeft && hasRight)
            {
                map[docId] = left!.Concat(right!).Distinct().Order().ToArray();
            }
            else if (hasLeft)
            {
                map[docId] = left!;
            }
            else
            {
                map[docId] = right!;
            }
        }

        return new MatchSet(map);
    }

    public MatchSet Not(IReadOnlyList<int> universe)
    {
        var docs = PostingListOperators.Negate(universe, ToDocPostingList());
        var map = docs.ToDictionary(static id => id, static _ => (IReadOnlyList<int>)Array.Empty<int>());
        return new MatchSet(map);
    }

    public MatchSet AdjacentOrdered(MatchSet other, int distance)
    {
        var docs = PostingListOperators.Intersect(ToDocPostingList(), other.ToDocPostingList());
        var map = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var docId in docs)
        {
            var positions = PostingListOperators.AdjacentOrdered(_positionsByDocument[docId], other._positionsByDocument[docId], distance);
            if (positions.Count > 0)
            {
                map[docId] = positions;
            }
        }

        return new MatchSet(map);
    }

    public MatchSet NearUnordered(MatchSet other, int distance)
    {
        var docs = PostingListOperators.Intersect(ToDocPostingList(), other.ToDocPostingList());
        var map = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var docId in docs)
        {
            var positions = PostingListOperators.NearUnordered(_positionsByDocument[docId], other._positionsByDocument[docId], distance);
            if (positions.Count > 0)
            {
                map[docId] = positions;
            }
        }

        return new MatchSet(map);
    }

    private PostingList ToDocPostingList()
    {
        var postings = _positionsByDocument.Keys
            .Order()
            .Select(static docId => new Posting(docId, []))
            .ToArray();
        return new PostingList(postings);
    }
}
