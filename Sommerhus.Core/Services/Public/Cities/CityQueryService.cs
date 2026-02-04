using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Cities;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Cities;

public sealed class CityQueryService : ICityQueryService
{
    private readonly AppDbContext db;

    public CityQueryService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IEnumerable<CityListItemDto>> GetAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto(c.Id, c.Name, c.Zip))
            .ToListAsync(ct);
}
