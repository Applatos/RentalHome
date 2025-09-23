using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class CitiesController(AppDbContext db) : ControllerBase
{
    public record CityListItemDto(string City, string Zip, int Count);

    [HttpGet]
    public async Task<IEnumerable<CityListItemDto>> Get(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .Select(c => new CityListItemDto(c.Name, c.Zip, c.Houses.Count))
            .ToListAsync(ct);
}
