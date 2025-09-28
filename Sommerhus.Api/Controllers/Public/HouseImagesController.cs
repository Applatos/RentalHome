using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Api.Models;
using Sommerhus.Api.Data;
using Sommerhus.Api.Utils;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/images")]
public class HouseImagesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> List(Guid houseId, CancellationToken ct)
    {
        var imgs = await db.Images
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.Id)
            .ToListAsync(ct);

        return imgs.Select(i =>
            new ImageDto(
                i.Id,
                UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)),
                i.Alt,
                i.Kind.ToString()));
    }
}
