using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Controllers.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Sommerhus.Api.Data;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CityListItemDto>> Get(CancellationToken ct)
    {
        var data = await db.Houses.AsNoTracking()
            .Where(h => h.City != null && h.City != "")
            .GroupBy(h => h.City!)
            .Select(g => new { City = g.Key, Count = g.Count() })
            .OrderBy(x => x.City)
            .ToListAsync(ct);

        return data.Select(x => new CityListItemDto(Slugify(x.City), x.City, x.Count));
    }

    private static string Slugify(string input)
    {
        var normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
        return Regex.Replace(noDiacritics, @"[^a-z0-9]+", "-").Trim('-');
    }
}
