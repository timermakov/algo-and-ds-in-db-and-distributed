namespace Hw5.SearchIndex.Indexing;

public static class PostingListOperators
{
    public static IReadOnlyList<int> Intersect(PostingList left, PostingList right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var matches = new List<int>();
        var li = 0;
        var ri = 0;

        while (li < left.Count && ri < right.Count)
        {
            var ld = left.DocumentIdAt(li);
            var rd = right.DocumentIdAt(ri);
            if (ld == rd)
            {
                matches.Add(ld);
                li++;
                ri++;
                continue;
            }

            if (ld < rd)
            {
                if (left.TryGetSkipIndex(li, out var skipIndex) && left.DocumentIdAt(skipIndex) <= rd)
                {
                    li = skipIndex;
                    continue;
                }

                li++;
                continue;
            }

            if (right.TryGetSkipIndex(ri, out var rightSkip) && right.DocumentIdAt(rightSkip) <= ld)
            {
                ri = rightSkip;
                continue;
            }

            ri++;
        }

        return matches;
    }

    public static IReadOnlyList<int> Union(PostingList left, PostingList right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var merged = new List<int>();
        var li = 0;
        var ri = 0;

        while (li < left.Count && ri < right.Count)
        {
            var ld = left.DocumentIdAt(li);
            var rd = right.DocumentIdAt(ri);
            if (ld == rd)
            {
                merged.Add(ld);
                li++;
                ri++;
            }
            else if (ld < rd)
            {
                merged.Add(ld);
                li++;
            }
            else
            {
                merged.Add(rd);
                ri++;
            }
        }

        while (li < left.Count)
        {
            merged.Add(left.DocumentIdAt(li));
            li++;
        }

        while (ri < right.Count)
        {
            merged.Add(right.DocumentIdAt(ri));
            ri++;
        }

        return merged;
    }

    public static IReadOnlyList<int> Negate(IReadOnlyList<int> sortedUniverse, PostingList excluded)
    {
        ArgumentNullException.ThrowIfNull(sortedUniverse);
        ArgumentNullException.ThrowIfNull(excluded);

        var result = new List<int>();
        var ui = 0;
        var ei = 0;
        while (ui < sortedUniverse.Count)
        {
            var doc = sortedUniverse[ui];
            if (ei >= excluded.Count)
            {
                result.Add(doc);
                ui++;
                continue;
            }

            var excludedDoc = excluded.DocumentIdAt(ei);
            if (doc == excludedDoc)
            {
                ui++;
                ei++;
            }
            else if (doc < excludedDoc)
            {
                result.Add(doc);
                ui++;
            }
            else
            {
                ei++;
            }
        }

        return result;
    }

    public static IReadOnlyList<int> AdjacentOrdered(IReadOnlyList<int> leftPositions, IReadOnlyList<int> rightPositions, int distance)
    {
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be positive.");
        }

        var result = new List<int>();
        var lp = 0;
        var rp = 0;
        while (lp < leftPositions.Count && rp < rightPositions.Count)
        {
            var diff = rightPositions[rp] - leftPositions[lp];
            if (diff == distance)
            {
                result.Add(rightPositions[rp]);
                lp++;
                rp++;
            }
            else if (diff < distance)
            {
                rp++;
            }
            else
            {
                lp++;
            }
        }

        return result;
    }

    public static IReadOnlyList<int> NearUnordered(IReadOnlyList<int> leftPositions, IReadOnlyList<int> rightPositions, int distance)
    {
        if (distance < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(distance), "Distance must be positive.");
        }

        var matches = new List<int>();
        var lp = 0;
        var rp = 0;
        while (lp < leftPositions.Count && rp < rightPositions.Count)
        {
            var diff = Math.Abs(leftPositions[lp] - rightPositions[rp]);
            if (diff <= distance)
            {
                matches.Add(Math.Max(leftPositions[lp], rightPositions[rp]));
                lp++;
                rp++;
                continue;
            }

            if (leftPositions[lp] < rightPositions[rp])
            {
                lp++;
            }
            else
            {
                rp++;
            }
        }

        return matches;
    }
}
