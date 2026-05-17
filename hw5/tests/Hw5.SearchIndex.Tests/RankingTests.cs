using Hw5.SearchIndex.Documents;
using Hw5.SearchIndex.Indexing;
using Hw5.SearchIndex.Ranking;
using Hw5.SearchIndex.Searching;

namespace Hw5.SearchIndex.Tests;

public sealed class RankingTests
{
    [Fact]
    public void Bm25AndTfIdfReturnTopK()
    {
        var index = new InMemoryPositionalIndex();
        index.AddDocument(new SearchDocument(1, "alpha alpha beta"));
        index.AddDocument(new SearchDocument(2, "alpha beta"));
        index.AddDocument(new SearchDocument(3, "beta gamma"));
        index.Seal();

        var service = new SearchService();
        var bm25 = service.Search(index, "alpha OR beta", topK: 2, rankingMode: RankingMode.Bm25);
        var tfidf = service.Search(index, "alpha OR beta", topK: 2, rankingMode: RankingMode.TfIdf);

        Assert.Equal(2, bm25.Count);
        Assert.Equal(2, tfidf.Count);
        Assert.Equal(1, bm25[0].DocumentId);
        Assert.Equal(1, tfidf[0].DocumentId);
    }
}
