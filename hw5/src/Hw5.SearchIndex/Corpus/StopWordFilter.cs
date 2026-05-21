namespace Hw5.SearchIndex.Corpus;

public static class StopWordFilter
{
    public static HashSet<string> LoadFromFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Stopwords file not found: {path}", path);
        }

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            set.Add(line.ToLowerInvariant());
        }

        return set;
    }

    public static string DefaultPath(string hw5Root) =>
        Path.Combine(hw5Root, "data", "stopwords-en.txt");

    public static bool IsStopWord(string term, HashSet<string>? stopWords) =>
        stopWords is not null && stopWords.Contains(term);
}
