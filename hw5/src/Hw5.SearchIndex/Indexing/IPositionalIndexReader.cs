namespace Hw5.SearchIndex.Indexing;

public interface IPositionalIndexReader
{
    int DocumentCount { get; }

    IReadOnlyCollection<int> AllDocumentIds { get; }

    IReadOnlyCollection<string> Terms { get; }

    PostingList GetPostings(string term);

    int GetDocumentLength(int documentId);
}
