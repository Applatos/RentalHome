using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Public.Areas;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class AreasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AreaListItemDto>> Get([FromQuery] string? q, CancellationToken ct)
    {
        IQueryable<Area> query = db.Areas.AsNoTracking()
            .Include(a => a.City)
            .Include(a => a.AreaImages)
            .Include(a => a.Houses).ThenInclude(h => h.Images);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var slugTerm = term.ToLowerInvariant();
            query = query.Where(a =>
                EF.Functions.Like(a.Name, $"%{term}%") ||
                EF.Functions.Like(a.Slug, $"%{slugTerm}%") ||
                (a.City != null && EF.Functions.Like(a.City.Name, $"%{term}%")));
        }

        var rows = await query
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Slug,
                a.Name,
                City = a.City != null ? a.City.Name : null,
                a.Description,
                HouseCount = a.Houses.Count,
                AreaHero = a.AreaImages
                    .OrderBy(img => img.SortOrder)
                    .ThenBy(img => img.Id)
                    .Select(img => img.FileName)
                    .FirstOrDefault(),
                HouseCover = a.Houses
                    .SelectMany(h => h.Images)
                    .Where(img => img.Kind == ImageKind.Cover || img.Kind == ImageKind.Gallery)
                    .OrderBy(img => img.Kind == ImageKind.Cover ? 0 : 1)
                    .ThenBy(img => img.Id)
                    .Select(img => new { img.HouseId, img.FileName })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        return rows.Select(r =>
        {
            string? relative = r.AreaHero is not null
                ? UrlBuilder.AreaImageWebPath(r.Id, r.AreaHero)
                : r.HouseCover is not null
                    ? UrlBuilder.HouseImageWebPath(r.HouseCover.HouseId, r.HouseCover.FileName)
                    : null;

            var heroUrl = relative is null ? null : UrlBuilder.ToAbsolute(Request, relative);
            var summary = Summarize(r.Description, 200);
            return new AreaListItemDto(r.Id, r.Slug, r.Name, r.City, r.HouseCount, summary, heroUrl);
        });
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<AreaDetailDto>> GetBySlug(string slug, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(slug)) return NotFound();

        var trimmed = slug.Trim();
        var normalized = trimmed.ToLowerInvariant();
        Guid? parsedId = Guid.TryParse(trimmed, out var guid) ? guid : null;

        var area = await db.Areas.AsNoTracking()
            .Include(a => a.City)
            .Include(a => a.AreaImages)
            .Include(a => a.Houses).ThenInclude(h => h.Images)
            .Include(a => a.Houses).ThenInclude(h => h.City)
            .FirstOrDefaultAsync(a => a.Slug == normalized || (parsedId.HasValue && a.Id == parsedId.Value), ct);

        if (area is null) return NotFound();

        var images = area.AreaImages
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new AreaImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(area.Id, i.FileName))))
            .ToList();

        var houses = area.Houses
            .OrderBy(h => h.Title)
            .Select(h =>
            {
                var cover = h.Images
                    .Where(i => i.Kind == ImageKind.Cover || i.Kind == ImageKind.Gallery)
                    .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : 1)
                    .ThenBy(i => i.Id)
                    .FirstOrDefault();

                var coverUrl = cover is null
                    ? null
                    : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(h.Id, cover.FileName));

                return new AreaHouseDto(h.Id, h.Title, h.Subtitle, h.City?.Name, h.City?.Zip, coverUrl);
            })
            .ToList();

        return new AreaDetailDto(
            area.Id,
            area.Slug,
            area.Name,
            area.City?.Name,
            area.Description,
            images,
            houses);
    }

    private static string? Summarize(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();
        if (trimmed.Length <= maxLength) return trimmed;
        return trimmed[..maxLength].TrimEnd() + "...";
    }
}
