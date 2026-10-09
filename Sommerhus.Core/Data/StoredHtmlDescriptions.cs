using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Core.Data;

/// <summary>
/// House and area descriptions are plain text: a blank line separates paragraphs and a single
/// line break stays a line break. Earlier releases stored HTML (the demo import wrote
/// <c>&lt;p&gt;</c> paragraphs), which the pages rendered raw. Startup converts any such
/// description to text, so the pages never need to trust stored markup.
/// </summary>
public static class StoredHtmlDescriptions
{
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    private static readonly Regex HtmlMarker = new(
        @"<\s*/?\s*(p|br|div|span|ul|ol|li|strong|em|b|i|h[1-6])\b[^>]*>", Options | RegexOptions.Compiled);

    public static bool LooksLikeHtml(string? text) => text is not null && HtmlMarker.IsMatch(text);

    public static string ToText(string html)
    {
        var text = html.Replace("\r\n", "\n");
        text = Regex.Replace(text, @"<\s*br\s*/?\s*>", "\n", Options);
        text = Regex.Replace(text, @"<\s*/\s*(p|div|ul|ol|h[1-6])\s*>", "\n\n", Options);
        text = Regex.Replace(text, @"<\s*li\b[^>]*>", "\n", Options);
        text = Regex.Replace(text, "<[^>]+>", string.Empty);
        text = WebUtility.HtmlDecode(text);

        var paragraphs = Regex.Split(text.Trim(), @"\n\s*\n")
            .Select(p => string.Join("\n", p.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0)))
            .Where(p => p.Length > 0);

        return string.Join("\n\n", paragraphs);
    }

    /// <returns>The houses whose description changed, so their search documents can be refreshed.</returns>
    public static async Task<IReadOnlyList<Guid>> ConvertAsync(AppDbContext db, CancellationToken ct)
    {
        // Narrowed in SQL to descriptions with a '<' at all; the precise check runs here.
        var houses = (await db.Houses.Where(h => h.Description != null && h.Description.Contains("<")).ToListAsync(ct))
            .Where(h => LooksLikeHtml(h.Description))
            .ToList();
        var areas = (await db.Areas.Where(a => a.Description != null && a.Description.Contains("<")).ToListAsync(ct))
            .Where(a => LooksLikeHtml(a.Description))
            .ToList();

        foreach (var house in houses)
            house.Description = ToText(house.Description!);
        foreach (var area in areas)
            area.Description = ToText(area.Description!);

        if (houses.Count > 0 || areas.Count > 0)
            await db.SaveChangesAsync(ct);

        return houses.Select(h => h.Id).ToList();
    }
}
