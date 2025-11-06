using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Public.Cities;
using Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Repository.Public.Cities;

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
