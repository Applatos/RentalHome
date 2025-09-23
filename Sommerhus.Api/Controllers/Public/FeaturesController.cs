using Sommerhus.Api.Dtos.Admin.Features;
using Sommerhus.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDto>> GetAll(CancellationToken ct)
        => (await db.Features.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct))
           .Select(f => new FeatureDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, f.IconUrl, f.SortOrder));
}
