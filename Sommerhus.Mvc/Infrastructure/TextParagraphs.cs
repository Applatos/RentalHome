using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// Renders a plain-text description as HTML paragraphs: a blank line starts a new paragraph and a
/// single line break becomes <c>&lt;br&gt;</c>. Every line is encoded, so stored text can never
/// inject markup.
/// </summary>
public static class TextParagraphs
{
    public static IHtmlContent Render(string? text, string? paragraphClass = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return HtmlString.Empty;
        }

        var builder = new HtmlContentBuilder();
        var classAttribute = string.IsNullOrWhiteSpace(paragraphClass)
            ? string.Empty
            : $" class=\"{HtmlEncoder.Default.Encode(paragraphClass)}\"";

        foreach (var paragraph in Regex.Split(text.Replace("\r\n", "\n").Trim(), @"\n\s*\n"))
        {
            var lines = paragraph.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            if (lines.Count == 0)
            {
                continue;
            }

            builder.AppendHtml($"<p{classAttribute}>");
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    builder.AppendHtml("<br>");
                }

                builder.Append(lines[i]);
            }

            builder.AppendHtml("</p>");
        }

        return builder;
    }
}
