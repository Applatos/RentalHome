using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.Cities;
using Sommerhus.Contracts.Dtos.Public.Cities;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Repository;

namespace Sommerhus.Repository.Admin.Cities;

public sealed class AdminCityService : IAdminCityService
{
    private readonly AppDbContext db;

    public AdminCityService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<CityListItemDto>> GetAllAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto(c.Id, c.Name, c.Zip))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(ct);
}
