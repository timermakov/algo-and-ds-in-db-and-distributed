namespace Hw5.SearchIndex.Indexing;

public sealed class PostingList
{
    private readonly int[] _documentIds;
    private readonly IReadOnlyList<int>[] _positions;
    private readonly Dictionary<int, int> _skipTable;

    public PostingList(IReadOnlyList<Posting> postings)
    {
        ArgumentNullException.ThrowIfNull(postings);

        _documentIds = new int[postings.Count];
        _positions = new IReadOnlyList<int>[postings.Count];

        for (var i = 0; i < postings.Count; i++)
        {
            _documentIds[i] = postings[i].DocumentId;
            _positions[i] = postings[i].Positions;
        }

        _skipTable = BuildSkipTable(postings.Count);
    }

    public int Count => _documentIds.Length;

    public int DocumentIdAt(int index) => _documentIds[index];

    public IReadOnlyList<int> PositionsAt(int index) => _positions[index];

    public bool TryGetSkipIndex(int index, out int skipIndex) => _skipTable.TryGetValue(index, out skipIndex);

    private static Dictionary<int, int> BuildSkipTable(int count)
    {
        var table = new Dictionary<int, int>();
        if (count <= 2)
        {
            return table;
        }

        var step = Math.Max(2, (int)Math.Sqrt(count));
        for (var i = 0; i + step < count; i += step)
        {
            table[i] = i + step;
        }

        return table;
    }
}
