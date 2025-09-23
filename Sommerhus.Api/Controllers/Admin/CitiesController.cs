using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Dtos.Admin.Cities;
using Sommerhus.Api.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using System.Text.RegularExpressions;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities")]
public sealed class CitiesController(AppDbContext db) : ControllerBase
{
    private static readonly Regex SlugRegex = new("^[a-z0-9-]+$", RegexOptions.Compiled);

    [HttpGet]
    public async Task<CityPageResult> List(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var q = db.Cities.AsNoTracking().Include(c => c.Images).Include(c => c.Houses).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c =>
                EF.Functions.Like(c.Name, $"%{term}%") ||
                EF.Functions.Like(c.Zip, $"%{term}%") ||
                EF.Functions.Like(c.Slug, $"%{term}%"));
        }

        var total = await q.CountAsync(ct);
        var rows = await q
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Zip)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CityListItemDto(
                c.Id,
                c.Name,
                c.Zip,
                c.Slug,
                c.Houses.Count,
                c.Images.Count))
            .ToListAsync(ct);

        return new CityPageResult
        {
            Query = query,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CityDetailDto>> Get(Guid id, CancellationToken ct)
    {
        var city = await db.Cities
            .Include(c => c.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (city is null) return NotFound();

        var images = city.Images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.CityImageWebPath(city.Id, i.FileName)), i.Alt, "city"))
            .ToArray();

        return new CityDetailDto(city.Id, city.Name, city.Zip, city.Slug, city.Description, images);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCityDto dto, CancellationToken ct)
    {
        var (name, zip, slug, text) = Normalize(dto);

        if (!SlugRegex.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(dto.Slug), "Slug må kun indeholde små bogstaver, tal og bindestreg");
            return ValidationProblem(ModelState);
        }

        var exists = await db.Cities.AnyAsync(c => c.Slug == slug, ct);
        if (exists)
        {
            ModelState.AddModelError(nameof(dto.Slug), "Slug skal være unik");
            return ValidationProblem(ModelState);
        }

        var city = new City
        {
            Name = name,
            Zip = zip,
            Slug = slug,
            Description = text
        };

        db.Cities.Add(city);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = city.Id }, city.Id);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCityDto dto, CancellationToken ct)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return NotFound();

        var (name, zip, slug, text) = Normalize(dto);

        if (!SlugRegex.IsMatch(slug))
        {
            ModelState.AddModelError(nameof(dto.Slug), "Slug må kun indeholde små bogstaver, tal og bindestreg");
            return ValidationProblem(ModelState);
        }

        var exists = await db.Cities
            .AnyAsync(c => c.Id != id && c.Slug == slug, ct);
        if (exists)
        {
            ModelState.AddModelError(nameof(dto.Slug), "Slug skal være unik");
            return ValidationProblem(ModelState);
        }

        city.Name = name;
        city.Zip = zip;
        city.Slug = slug;
        city.Description = text;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var city = await db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (city is null) return NotFound();

        db.Cities.Remove(city);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static (string Name, string Zip, string Slug, string? Text) Normalize(CreateCityDto dto)
    {
        return (
            dto.Name.Trim(),
            dto.Zip.Trim(),
            dto.Slug.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(dto.Text) ? null : dto.Text.Trim());
    }

    private static (string Name, string Zip, string Slug, string? Text) Normalize(UpdateCityDto dto)
        => Normalize(new CreateCityDto(dto.Name, dto.Zip, dto.Slug, dto.Text));
}
