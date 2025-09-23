using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Houses;
using Sommerhus.Api.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesQueryController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PageResult<HouseListItemDto>> Get(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = db.Houses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Subtitle != null && EF.Functions.Like(h.Subtitle, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderBy(h => h.City.Name).ThenBy(h => h.Title).ThenBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.Title,
                City = h.City.Name,
                Zip = h.City.Zip,
                CoverFile = h.Images
                    .OrderBy(i => i.Kind == ImageKind.Cover ? 0 :
                                  i.Kind == ImageKind.Gallery ? 1 : 2)
                    .Select(i => i.FileName)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            string? coverUrl = r.CoverFile is null ? null : UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(r.Id, r.CoverFile));
            return new HouseListItemDto(r.Id, r.Title, r.City, r.Zip, coverUrl);
        }).ToList();

        return new PageResult<HouseListItemDto>
        {
            Query = query,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }
}
