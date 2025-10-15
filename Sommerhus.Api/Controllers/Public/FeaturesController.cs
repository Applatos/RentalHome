using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Contracts.Dtos.Admin.Features; // genbruger DTO - fint til public
using Sommerhus.Api.Utils;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/[controller]")]
public class FeaturesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<FeatureDetailsDto>> GetAll(CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var icon = string.IsNullOrWhiteSpace(f.IconUrl) ? null
                : UrlBuilder.ToAbsolute(Request, f.IconUrl!);
            return new FeatureDetailsDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, icon);
        });
    }
}
