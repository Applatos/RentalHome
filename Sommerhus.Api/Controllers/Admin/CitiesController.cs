using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/cities")]
public sealed class CitiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CityListItemDto>> GetAll(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto(c.Id, c.Slug, c.Name, c.Zip))
            .ToListAsync(ct);
}
