using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDetailsDto>> GetAll(CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var icon = storage.GetUrl(Request, f.IconUrl);
            return new FeatureDetailsDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, icon);
        });
    }
}
