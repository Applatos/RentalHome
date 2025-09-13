// Sommerhus.Api/Controllers/Admin/HousesQueryController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesQueryController(AppDbContext db) : ControllerBase
{
    // DTO'er til pagingen
    public sealed record HouseListItem(Guid Id, string Title, string? City, string? Zip, string? Cover);

    public sealed class HousesPageDto
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<HouseListItem> Items { get; set; } = new();
    }

    private static string ImgPath(Guid houseId, string fileName)
    {
        return $"/uploads/houses/{houseId}/{fileName}";
    } 

    private string AbsUrl(string relative)
    {
        var req = HttpContext?.Request;
        if (req is null) return relative;
        return $"{req.Scheme}://{req.Host}{req.PathBase}{relative}";
    }

    [HttpGet]
    public async Task<HousesPageDto> Get([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 3, CancellationToken ct = default)
    {

        var q = db.Houses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Subtitle != null && EF.Functions.Like(h.Subtitle, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City, $"%{term}%")) ||
                (h.Zip != null && EF.Functions.Like(h.Zip, $"%{term}%"))
            );
        }

        var total = await q.CountAsync(ct);

        var rows = await q
            .OrderBy(h => h.City).ThenBy(h => h.Title).ThenBy(h => h.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.Title,
                h.City,
                h.Zip,
                CoverFile = h.Images
                    .OrderBy(i => i.Kind == ImageKind.Cover ? 0 :
                                  i.Kind == ImageKind.Gallery ? 1 : 2)
                    .Select(i => i.FileName)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            string? url = r.CoverFile is null ? null : AbsUrl(ImgPath(r.Id, r.CoverFile));
            return new HouseListItem(r.Id, r.Title, r.City, r.Zip, url);
        }).ToList();

        return new HousesPageDto
        {
            Query = query,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }
}
