using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Houses;
using Sommerhus.Api.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using System.IO;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private readonly IWebHostEnvironment _env = env;

    [HttpGet]
    public async Task<PageResult<HouseListItemDto>> Get(
        [FromQuery] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = db.Houses.AsNoTracking()
            .Include(h => h.Images)
            .Include(h => h.City)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.Subtitle != null && EF.Functions.Like(h.Subtitle, $"%{term}%")) ||
                (h.Description != null && EF.Functions.Like(h.Description, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City.Zip, $"%{term}%")) ||
                (h.City != null && EF.Functions.Like(h.City.Slug, $"%{term}%")));
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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var house = await db.Houses.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id, ct);
        if (house is null) return NotFound();

        var dto = new HouseDetailsDto(
            house.Id,
            house.Title,
            house.Subtitle,
            house.Address,
            house.CityId,
            house.Description,
            house.Facilities,
            house.AreaId,
            house.CoverImageId);

        return dto;
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var cityExists = await db.Cities.AnyAsync(c => c.Id == dto.CityId, ct);
        if (!cityExists)
        {
            ModelState.AddModelError(nameof(dto.CityId), "By findes ikke");
            return ValidationProblem(ModelState);
        }

        var house = new VacationHouse
        {
            Title = dto.Title.Trim(),
            Subtitle = Clean(dto.Subtitle),
            Address = Clean(dto.Address),
            CityId = dto.CityId,
            Description = Clean(dto.Description),
            Facilities = Clean(dto.Facilities),
            CreatedUtc = DateTime.UtcNow
        };

        db.Houses.Add(house);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = house.Id }, house.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (house is null) return NotFound();

        var cityExists = await db.Cities.AnyAsync(c => c.Id == dto.CityId, ct);
        if (!cityExists)
        {
            ModelState.AddModelError(nameof(dto.CityId), "By findes ikke");
            return ValidationProblem(ModelState);
        }

        house.Title = dto.Title.Trim();
        house.Subtitle = Clean(dto.Subtitle);
        house.Address = Clean(dto.Address);
        house.CityId = dto.CityId;
        house.Description = Clean(dto.Description);
        house.Facilities = Clean(dto.Facilities);

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == id, ct);
        if (house is null) return NotFound();

        var imageFiles = await db.Images
            .Where(i => i.HouseId == id)
            .Select(i => i.FileName)
            .ToListAsync(ct);

        db.Houses.Remove(house);
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
        {
            var houseDir = Path.Combine(_env.WebRootPath, "uploads", "houses", id.ToString());

            foreach (var fileName in imageFiles.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                var fullPath = Path.Combine(houseDir, fileName!);
                try
                {
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            try
            {
                if (Directory.Exists(houseDir) && !Directory.EnumerateFileSystemEntries(houseDir).Any())
                {
                    Directory.Delete(houseDir);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return NoContent();
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
