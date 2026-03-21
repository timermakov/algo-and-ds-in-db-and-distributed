using System.Text;

namespace Hw1.Algorithms.Lsh;

public sealed class TextLshIndex
{
    private readonly LshConfig _config;
    private readonly ulong[] _seeds;
    private readonly Dictionary<string, StoredDocument> _documents = new(StringComparer.Ordinal);
    private readonly Dictionary<ulong, HashSet<string>>[] _buckets;

    public TextLshIndex(LshConfig config)
    {
        _config = config;
        _config.Validate();
        _seeds = BuildSeeds(_config.NumHashes);
        _buckets = new Dictionary<ulong, HashSet<string>>[_config.Bands];
        for (var i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = [];
        }
    }

    public int Count => _documents.Count;

    public void BuildIndex(IEnumerable<TextDocument> docs)
    {
        ArgumentNullException.ThrowIfNull(docs);
        foreach (var doc in docs)
        {
            AddDocument(doc);
        }
    }

    public void AddDocument(TextDocument doc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(doc.Id);
        if (_documents.ContainsKey(doc.Id))
        {
            throw new InvalidOperationException($"Document '{doc.Id}' already exists.");
        }

        var shingles = BuildShingles(doc.Text, _config.ShingleSize);
        var signature = BuildSignature(shingles);
        _documents.Add(doc.Id, new StoredDocument(shingles));

        var rowsPerBand = RowsPerBand();
        for (var band = 0; band < _config.Bands; band++)
        {
            var key = BandKey(signature, band * rowsPerBand, rowsPerBand);
            if (!_buckets[band].TryGetValue(key, out var ids))
            {
                ids = [];
                _buckets[band][key] = ids;
            }

            ids.Add(doc.Id);
        }
    }

    public IReadOnlyList<TextMatch> FindDuplicatesLsh(string text, double threshold = 0)
    {
        var limit = ResolveThreshold(threshold);
        var queryShingles = BuildShingles(text, _config.ShingleSize);
        var candidates = CandidateIds(text);
        var matches = new List<TextMatch>(candidates.Count);

        foreach (var candidateId in candidates)
        {
            var score = Jaccard(queryShingles, _documents[candidateId].Shingles);
            if (score >= limit)
            {
                matches.Add(new TextMatch(candidateId, score));
            }
        }

        matches.Sort(TextMatchComparer.Instance);
        return matches;
    }

    public IReadOnlyList<TextMatch> FindDuplicatesFullScan(string text, double threshold = 0)
    {
        var limit = ResolveThreshold(threshold);
        var queryShingles = BuildShingles(text, _config.ShingleSize);
        var matches = new List<TextMatch>(_documents.Count);

        foreach (var pair in _documents)
        {
            var score = Jaccard(queryShingles, pair.Value.Shingles);
            if (score >= limit)
            {
                matches.Add(new TextMatch(pair.Key, score));
            }
        }

        matches.Sort(TextMatchComparer.Instance);
        return matches;
    }

    public IReadOnlyList<string> CandidateIds(string text)
    {
        var shingles = BuildShingles(text, _config.ShingleSize);
        var signature = BuildSignature(shingles);
        var rowsPerBand = RowsPerBand();
        var result = new HashSet<string>(StringComparer.Ordinal);

        for (var band = 0; band < _config.Bands; band++)
        {
            var key = BandKey(signature, band * rowsPerBand, rowsPerBand);
            if (!_buckets[band].TryGetValue(key, out var ids))
            {
                continue;
            }

            foreach (var id in ids)
            {
                result.Add(id);
            }
        }

        return result.OrderBy(x => x, StringComparer.Ordinal).ToArray();
    }

    private int RowsPerBand() => _config.NumHashes / _config.Bands;

    private double ResolveThreshold(double threshold)
    {
        if (threshold == 0)
        {
            return _config.SimilarityThreshold;
        }

        if (threshold < 0)
        {
            return 0;
        }

        if (threshold > 1)
        {
            return 1;
        }

        return threshold;
    }

    private ulong[] BuildSignature(HashSet<ulong> shingles)
    {
        var signature = new ulong[_config.NumHashes];
        for (var i = 0; i < signature.Length; i++)
        {
            var minimum = ulong.MaxValue;
            var seed = _seeds[i];
            foreach (var shingle in shingles)
            {
                var value = Mix64(shingle ^ seed);
                if (value < minimum)
                {
                    minimum = value;
                }
            }

            signature[i] = minimum;
        }

        return signature;
    }

    private static HashSet<ulong> BuildShingles(string text, int shingleSize)
    {
        var tokens = Tokenize(text);
        if (tokens.Count == 0)
        {
            return [HashString64("<empty>")];
        }

        if (tokens.Count < shingleSize)
        {
            return [HashString64(string.Join(' ', tokens))];
        }

        var result = new HashSet<ulong>();
        for (var i = 0; i + shingleSize <= tokens.Count; i++)
        {
            result.Add(HashString64(string.Join(' ', tokens.GetRange(i, shingleSize))));
        }

        return result;
    }

    private static List<string> Tokenize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static ulong BandKey(ulong[] signature, int start, int length)
    {
        var hash = 1469598103934665603UL;
        for (var i = 0; i < length; i++)
        {
            hash ^= signature[start + i];
            hash *= 1099511628211UL;
        }

        return Mix64(hash);
    }

    private static double Jaccard(HashSet<ulong> left, HashSet<ulong> right)
    {
        if (left.Count == 0 && right.Count == 0)
        {
            return 1;
        }

        var intersection = 0;
        foreach (var value in left)
        {
            if (right.Contains(value))
            {
                intersection++;
            }
        }

        var union = left.Count + right.Count - intersection;
        return union == 0 ? 1 : (double)intersection / union;
    }

    private static ulong[] BuildSeeds(int count)
    {
        var seeds = new ulong[count];
        var current = 0x9e3779b97f4a7c15UL;
        for (var i = 0; i < count; i++)
        {
            current = Mix64(current + (ulong)i + 0x100000001b3UL);
            seeds[i] = current;
        }

        return seeds;
    }

    private static ulong HashString64(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = 1469598103934665603UL;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= 1099511628211UL;
        }

        return Mix64(hash);
    }

    private static ulong Mix64(ulong x)
    {
        x ^= x >> 33;
        x *= 0xff51afd7ed558ccdUL;
        x ^= x >> 33;
        x *= 0xc4ceb9fe1a85ec53UL;
        x ^= x >> 33;
        return x;
    }

    private sealed record StoredDocument(HashSet<ulong> Shingles);
}

public sealed record LshConfig(int NumHashes = 64, int Bands = 8, int ShingleSize = 2, double SimilarityThreshold = 0.8)
{
    public void Validate()
    {
        if (NumHashes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(NumHashes), "NumHashes must be positive.");
        }

        if (Bands <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Bands), "Bands must be positive.");
        }

        if (ShingleSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ShingleSize), "ShingleSize must be positive.");
        }

        if (NumHashes % Bands != 0)
        {
            throw new ArgumentException("NumHashes must be divisible by Bands.");
        }

        if (SimilarityThreshold < 0 || SimilarityThreshold > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(SimilarityThreshold), "SimilarityThreshold must be in [0, 1].");
        }
    }
}

public sealed record TextDocument(string Id, string Text);

public sealed record TextMatch(string Id, double Score);

internal sealed class TextMatchComparer : IComparer<TextMatch>
{
    public static readonly TextMatchComparer Instance = new();

    public int Compare(TextMatch? x, TextMatch? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return 1;
        }

        if (y is null)
        {
            return -1;
        }

        var byScore = y.Score.CompareTo(x.Score);
        return byScore != 0 ? byScore : StringComparer.Ordinal.Compare(x.Id, y.Id);
    }
}
