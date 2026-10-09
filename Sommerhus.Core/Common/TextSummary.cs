using System.Text.RegularExpressions;

namespace Sommerhus.Core.Common;

/// <summary>
/// A one-line summary of a description for cards and lists: the whole text when it is short,
/// otherwise its first sentence, otherwise its first 160 characters.
/// </summary>
public static class TextSummary
{
    private const int MaxLength = 160;

    private static readonly Regex HtmlTagRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

    public static string? Build(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var withoutHtml = HtmlTagRegex.Replace(source, " ");
        var normalized = WhitespaceRegex.Replace(withoutHtml, " ").Trim();

        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length <= MaxLength)
        {
            return normalized;
        }

        foreach (var endChar in new[] { '.', '!', '?' })
        {
            var sentenceEnd = normalized.IndexOf(endChar);
            if (sentenceEnd >= 0 && sentenceEnd + 1 <= MaxLength)
            {
                return normalized[..(sentenceEnd + 1)].Trim();
            }
        }

        return normalized[..MaxLength].TrimEnd();
    }
}
