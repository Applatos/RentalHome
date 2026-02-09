using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Areas;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Areas;

public sealed class AreaQueryService : IAreaQueryService
{
    private readonly AppDbContext db;
    private readonly IImageStorage storage;

    public AreaQueryService(AppDbContext db, IImageStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    public async Task<IEnumerable<AreaListItemDto>> SearchAsync(string? query, CancellationToken ct)
    {
        var areaQuery = db.Areas.AsNoTracking()
            .Where(a => a.Status == EntityStatus.Published)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            areaQuery = areaQuery.Where(a => EF.Functions.Like(a.Name, $"%{term}%"));
        }

        return await areaQuery
            .OrderBy(a => a.Name)
            .Select(a => new AreaListItemDto(a.Id, a.Name, a.Houses.Count))
            .ToListAsync(ct);
    }

    public async Task<AreaDetailsDto?> GetAsync(Guid id, string baseUrl, CancellationToken ct)
    {
        var area = await db.Areas
            .Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstOrDefaultAsync(a => a.Id == id && a.Status == EntityStatus.Published, ct);

        if (area is null)
        {
            return null;
        }

        var houses = area.Houses
            .OrderBy(h => h.Title)
            .ThenBy(h => h.Id)
            .Select(h => new AreaHouseDto(h.Id, h.Title))
            .ToList();

        var images = area.AreaImages
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, storage.GetUrl(baseUrl, ImageCategory.Area, area.Id, i.FileName), null, "Gallery"))
            .ToList();

        var cityItems = area.Cities
            .OrderBy(c => c.Zip)
            .ThenBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, $"{c.Zip} – {c.Name}"))
            .ToList();

        var cityIds = cityItems.Select(c => c.Id).ToList();

        return new AreaDetailsDto(
            area.Id,
            area.Name,
            area.Description,
            images,
            
            // Admin-specific fields (not used in public)
            CityIds: cityIds,
            Cities: cityItems,
            Houses: houses);
    }
}
