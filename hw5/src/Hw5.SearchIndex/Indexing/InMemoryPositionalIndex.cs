using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Tokenization;

namespace Hw5.SearchIndex.Indexing;

public sealed class InMemoryPositionalIndex
{
    private readonly Dictionary<string, List<Posting>> _termPostings = new(StringComparer.Ordinal);
    private readonly Dictionary<int, SearchDocument> _documents = new();

    public int DocumentCount => _documents.Count;

    public IReadOnlyCollection<int> AllDocumentIds => _documents.Keys;

    public void AddDocument(SearchDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (_documents.ContainsKey(document.Id))
        {
            throw new InvalidOperationException($"Document id {document.Id} already exists.");
        }

        _documents[document.Id] = document;
        var tokens = SimpleTokenizer.Tokenize(document.Text);
        var termPositions = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            if (!termPositions.TryGetValue(token.Term, out var positions))
            {
                positions = [];
                termPositions[token.Term] = positions;
            }

            positions.Add(token.Position);
        }

        foreach (var pair in termPositions)
        {
            if (!_termPostings.TryGetValue(pair.Key, out var postings))
            {
                postings = [];
                _termPostings[pair.Key] = postings;
            }

            postings.Add(new Posting(document.Id, pair.Value));
        }
    }

    public void Seal()
    {
        foreach (var postings in _termPostings.Values)
        {
            postings.Sort(static (a, b) => a.DocumentId.CompareTo(b.DocumentId));
        }
    }

    public PostingList GetPostings(string term)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        var normalized = term.ToLowerInvariant();
        if (!_termPostings.TryGetValue(normalized, out var postings))
        {
            return new PostingList([]);
        }

        return new PostingList(postings);
    }

    public bool TryGetDocument(int id, out SearchDocument document) => _documents.TryGetValue(id, out document!);

    public IReadOnlyCollection<string> Terms => _termPostings.Keys;
}
