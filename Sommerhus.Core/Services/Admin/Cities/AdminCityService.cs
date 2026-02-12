using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Cities;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core;

namespace Sommerhus.Core.Services.Admin.Cities;

public sealed class AdminCityService(AppDbContext db) : IAdminCityService
{

    public async Task<IReadOnlyList<CityDto>> GetAllAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityDto(c.Id, c.Name, c.Zip, null, null, null, null))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(ct);
}
