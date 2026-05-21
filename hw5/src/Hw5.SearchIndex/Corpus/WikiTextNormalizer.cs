using System.Text.RegularExpressions;

namespace Hw5.SearchIndex.Corpus;

public static partial class WikiTextNormalizer
{
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTag();

    [GeneratedRegex(@"\{\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}\}", RegexOptions.Compiled)]
    private static partial Regex Templates();

    [GeneratedRegex(@"\[\[(?:[^\]|]*\|)?([^\]]+)\]\]", RegexOptions.Compiled)]
    private static partial Regex WikiLinks();

    [GeneratedRegex(@"'{2,5}", RegexOptions.Compiled)]
    private static partial Regex BoldItalic();

    public static string ToPlainText(string wikitext)
    {
        if (string.IsNullOrWhiteSpace(wikitext))
        {
            return string.Empty;
        }

        var text = wikitext;
        text = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        text = Templates().Replace(text, " ");
        text = WikiLinks().Replace(text, "$1");
        text = text.Replace("[[", " ", StringComparison.Ordinal)
            .Replace("]]", " ", StringComparison.Ordinal);
        text = HtmlTag().Replace(text, " ");
        text = BoldItalic().Replace(text, " ");
        text = text.Replace("&nbsp;", " ", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal)
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal);
        text = Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}
