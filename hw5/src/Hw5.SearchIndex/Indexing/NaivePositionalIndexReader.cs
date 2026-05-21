using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Tokenization;

namespace Hw5.SearchIndex.Indexing;

/// <summary>
/// Scans all documents on every posting lookup — baseline for benchmarks only.
/// </summary>
public sealed class NaivePositionalIndexReader : IPositionalIndexReader
{
    private readonly IReadOnlyList<NaiveDocument> _documents;

    public NaivePositionalIndexReader(IEnumerable<SearchDocument> documents)
    {
        _documents = documents
            .Select(static d => new NaiveDocument(d.Id, SimpleTokenizer.Tokenize(d.Text)))
            .OrderBy(static d => d.Id)
            .ToArray();
    }

    public int DocumentCount => _documents.Count;

    public IReadOnlyCollection<int> AllDocumentIds => _documents.Select(static d => d.Id).ToArray();

    public IReadOnlyCollection<string> Terms =>
        _documents.SelectMany(static d => d.Tokens.Select(static t => t.Term)).Distinct(StringComparer.Ordinal).ToArray();

    public PostingList GetPostings(string term)
    {
        var postings = new List<Posting>();
        foreach (var doc in _documents)
        {
            var positions = new List<int>();
            foreach (var token in doc.Tokens)
            {
                if (string.Equals(token.Term, term, StringComparison.Ordinal))
                {
                    positions.Add(token.Position);
                }
            }

            if (positions.Count > 0)
            {
                postings.Add(new Posting(doc.Id, positions));
            }
        }

        postings.Sort(static (a, b) => a.DocumentId.CompareTo(b.DocumentId));
        return new PostingList(postings);
    }

    public int GetDocumentLength(int documentId)
    {
        var doc = _documents.FirstOrDefault(d => d.Id == documentId);
        return doc?.Tokens.Count ?? 0;
    }

    private sealed record NaiveDocument(int Id, IReadOnlyList<TokenPosition> Tokens);
}
