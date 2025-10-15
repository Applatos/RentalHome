using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Public.Areas;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class AreasController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<AreaListItemDto>> Get([FromQuery] string? q, CancellationToken ct)
    {
        var query = db.Areas.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            var slugTerm = term.ToLowerInvariant();
            query = query.Where(a =>
                EF.Functions.Like(a.Name, $"%{term}%") ||
                (a.City != null && EF.Functions.Like(a.City.Name, $"%{term}%")));
        }

        return await query
            .OrderBy(a => a.Name)
            .Select(a => new AreaListItemDto(a.Id, a.Name, a.Houses.Count))
            .ToListAsync(ct);
    }

    //[HttpGet("{slug}")]
    //public async Task<ActionResult<AreaDetailsDto>> GetBySlug(string slug, CancellationToken ct)
    //{
    //    var area = await db.Areas
    //        .Include(a => a.AreaImages)
    //        .FirstOrDefaultAsync(a => a.Slug == slug, ct);

    //    if (area is null) return NotFound();

    //    var images = area.AreaImages
    //        .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
    //        .Select(i => new ImageDto(i.Id,
    //            UrlBuilder.ToAbsolute(Request, UrlBuilder.AreaImageWebPath(area.Id, i.FileName)),
    //            null, "Gallery"))
    //        .ToList();

    //    return new AreaDetailsDto(area.Id, area.Slug, area.Name, area.Description, images);
    //}
}
