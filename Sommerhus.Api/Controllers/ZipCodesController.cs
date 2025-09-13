using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Controllers.Models;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ZipCodesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ZipCodeDto>> Search([FromQuery] string? filter, CancellationToken ct)
    {
        var q = db.ZipCodes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var f = filter.Trim();
            q = q.Where(z => z.Zip.Contains(f) || z.City.Contains(f));
        }

        return await q
            .OrderBy(z => z.Zip).ThenBy(z => z.City)
            .Select(z => new ZipCodeDto(z.Zip, z.City))
            .Take(50)
            .ToListAsync(ct);
    }
}
