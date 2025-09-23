using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/zipcodes")]
public class ZipCodesController(AppDbContext db) : ControllerBase
{
    public record ZipCodeDto(string Zip, string City);

    [HttpGet]
    public async Task<IEnumerable<ZipCodeDto>> Get([FromQuery] string? filter, CancellationToken ct)
    {
        var q = db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim();
            q = q.Where(c =>
                EF.Functions.Like(c.Zip, $"%{term}%") ||
                EF.Functions.Like(c.Name, $"%{term}%") ||
                EF.Functions.Like(c.Slug, $"%{term}%"));
        }

        return await q
            .OrderBy(c => c.Zip)
            .ThenBy(c => c.Name)
            .Take(20)
            .Select(c => new ZipCodeDto(c.Zip, c.Name))
            .ToListAsync(ct);
    }
}
