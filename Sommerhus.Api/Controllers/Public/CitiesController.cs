using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Repository;
using Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class CitiesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<CityListItemDto>> Get(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CityListItemDto(c.Id, c.Name, c.Zip))
            .ToListAsync(ct);
}
