using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Api.Controllers.Public;

[ApiController]
[Route("api/houses/{houseId:guid}/images")]
public class HouseImagesController(AppDbContext db, IImageStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<ImageDto>> List(Guid houseId, CancellationToken ct)
    {
        var images = await db.Images.AsNoTracking()
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .Select(i => new { i.Id, i.FileName, i.Alt, i.Kind })
            .ToListAsync(ct);

        return images.Select(i =>
            new ImageDto(
                i.Id,
                storage.GetUrl(Request, ImageCategory.House, houseId, i.FileName),
                i.Alt,
                i.Kind.ToString()));
    }
}
