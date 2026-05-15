namespace Hw5.SearchIndex.Querying;

public sealed class QueryParseException : Exception
{
    public QueryParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
