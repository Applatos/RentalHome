using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Images;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Core.Common;

namespace Sommerhus.Core.Services.Public.Images;

public sealed class HouseImageQueryService(AppDbContext db, IImageStorage storage) : IHouseImageQueryService
{

    public async Task<IEnumerable<ImageDto>> GetAsync(Guid houseId, string baseUrl, CancellationToken ct)
    {
        var images = await db.Images.AsNoTracking()
            .Where(i => i.HouseId == houseId)
            .InDisplayOrder()
            .Select(i => new { i.Id, i.FileName, i.Alt, i.Kind })
            .ToListAsync(ct);

        return images.Select(i =>
            new ImageDto(
                i.Id,
                storage.GetUrl(baseUrl, ImageCategory.House, houseId, i.FileName),
                i.Alt,
                i.Kind.ToString()))
            .ToList();
    }
}
