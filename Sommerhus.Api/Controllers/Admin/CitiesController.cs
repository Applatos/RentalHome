using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
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
    public async Task<IReadOnlyList<LookupItem>> GetCities(CancellationToken ct)
    => await db.Cities
        .OrderBy(c => c.Zip)
        .Select(c => new LookupItem(c.Id, c.Zip + " – " + c.Name))
        .ToListAsync(ct);

    [HttpGet("search")]
    public async Task<IReadOnlyList<LookupItem>> Search(string term, CancellationToken ct)
    {
        term = term?.Trim() ?? "";
        return await db.Cities
            .Where(c => c.Name.Contains(term) || c.Zip.Contains(term))
            .OrderBy(c => c.Zip).Take(20)
            .Select(c => new LookupItem(c.Id, c.Zip + " – " + c.Name))
            .ToListAsync(ct);
    }

}

