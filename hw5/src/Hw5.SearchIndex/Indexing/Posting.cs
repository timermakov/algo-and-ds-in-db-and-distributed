namespace Hw5.SearchIndex.Indexing;

public sealed class Posting
{
    public Posting(int documentId, IReadOnlyList<int> positions)
    {
        DocumentId = documentId;
        Positions = positions;
    }

    public int DocumentId { get; }

    public IReadOnlyList<int> Positions { get; }
}
