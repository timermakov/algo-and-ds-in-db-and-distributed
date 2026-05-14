using System.Globalization;
using System.Text;

namespace Hw5.SearchIndex.Tokenization;

public static class SimpleTokenizer
{
    public static IReadOnlyList<TokenPosition> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokens = new List<TokenPosition>();
        var builder = new StringBuilder();
        var position = 0;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
                continue;
            }

            if (builder.Length > 0)
            {
                tokens.Add(new TokenPosition(builder.ToString(), position));
                builder.Clear();
                position++;
            }
        }

        if (builder.Length > 0)
        {
            tokens.Add(new TokenPosition(builder.ToString(), position));
        }

        return tokens;
    }
}
