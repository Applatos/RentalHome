using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Contracts.Dtos.Public.Cities;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities")]
public sealed class CitiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CityListItemDto>> GetAll(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto(c.Id, c.Name, c.Zip))
            .ToListAsync(ct);

    [HttpGet("lookup")]
    public async Task<IEnumerable<LookupItem>> Lookup(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, c.Name))
            .ToListAsync(ct);
}
