//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Sommerhus.Api.Data;
//using Sommerhus.Api.Dtos;
//using System.Globalization;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace Sommerhus.Api.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class LocationsController : ControllerBase
//{
//    private readonly AppDbContext _db;
//    public LocationsController(AppDbContext db) => _db = db;

//    // Returnér alle unikke (City, Zip) par + antal huse pr. par
//    [HttpGet]
//    public async Task<IEnumerable<LocationDto>> GetAll(CancellationToken ct)
//    {
//        var rows = await _db.VacationHouses
//            .AsNoTracking()
//            .Where(h => !string.IsNullOrWhiteSpace(h.City) && !string.IsNullOrWhiteSpace(h.Zip))
//            .GroupBy(h => new { City = h.City!, Zip = h.Zip! })
//            .Select(g => new { g.Key.City, g.Key.Zip, Count = g.Count() })
//            .OrderBy(x => x.City)
//            .ToListAsync(ct);

//        return rows.Select(x => new LocationDto(x.City, x.Zip, Slugify(x.City), x.Count));
//    }

//    // Autocomplete-søgning: matcher både bynavn (med diakritik-fjernelse) og postnr.
//    // Prioritet: starts-with før contains, flest huse før færrest.
//    [HttpGet("search")]
//    public async Task<IEnumerable<LocationDto>> Search([FromQuery] string term, [FromQuery] int limit = 8, CancellationToken ct = default)
//    {
//        term ??= "";
//        var tSlug = Slugify(term);
//        var tDigits = new string(term.Where(char.IsDigit).ToArray());

//        var rows = await _db.VacationHouses
//            .AsNoTracking()
//            .Where(h => !string.IsNullOrWhiteSpace(h.City) && !string.IsNullOrWhiteSpace(h.Zip))
//            .GroupBy(h => new { City = h.City!, Zip = h.Zip! })
//            .Select(g => new { g.Key.City, g.Key.Zip, Count = g.Count() })
//            .ToListAsync(ct);

//        var items = rows.Select(x => new LocationDto(x.City, x.Zip, Slugify(x.City), x.Count));

//        // filter + sortering
//        var q = items
//            .Where(x =>
//                (tSlug.Length == 0) ||
//                x.Slug.Contains(tSlug, StringComparison.Ordinal) ||
//                x.Zip.Contains(tDigits, StringComparison.Ordinal))
//            .Select(x =>
//            {
//                var posCity = x.Slug.IndexOf(tSlug, StringComparison.Ordinal);
//                var posZip = string.IsNullOrEmpty(tDigits) ? int.MaxValue : x.Zip.IndexOf(tDigits, StringComparison.Ordinal);
//                var starts = (posCity == 0) || (posZip == 0);
//                var firstPos = Math.Min(posCity < 0 ? int.MaxValue : posCity, posZip);
//                return new { Item = x, Starts = starts, Pos = firstPos };
//            })
//            .OrderByDescending(x => x.Starts)
//            .ThenBy(x => x.Pos)
//            .ThenByDescending(x => x.Item.Count)
//            .ThenBy(x => x.Item.City)
//            .Take(limit)
//            .Select(x => x.Item);

//        return q;
//    }

//    // Simpel slug af bynavn (små bogstaver, fjern diakritik, ikke-alfanum → '-')
//    private static string Slugify(string input)
//    {
//        var normalized = (input ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
//        var sb = new StringBuilder();
//        foreach (var ch in normalized)
//        {
//            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
//            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
//        }
//        var noDiacritics = sb.ToString().Normalize(NormalizationForm.FormC);
//        return Regex.Replace(noDiacritics, @"[^a-z0-9]+", "-").Trim('-');
//    }
//}
