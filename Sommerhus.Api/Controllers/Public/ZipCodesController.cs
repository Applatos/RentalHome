using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Repository;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public sealed class ZipCodesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<string>> Find([FromQuery] string q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return [];
        var term = q.Trim();
        return await db.Cities.AsNoTracking()
            .Where(c => c.Zip != null && EF.Functions.Like(c.Zip, $"{term}%"))
            .OrderBy(c => c.Zip)
            .Select(c => c.Zip!)
            .Distinct()
            .Take(20)
            .ToListAsync(ct);
    }
}
