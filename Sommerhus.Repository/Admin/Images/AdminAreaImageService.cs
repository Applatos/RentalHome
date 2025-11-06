using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.Images;
using Sommerhus.Application.Common;
using Sommerhus.Application.Storage;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Repository.Admin.Images;

public sealed class AdminAreaImageService : AdminImageServiceBase, IAdminAreaImageService
{
    public AdminAreaImageService(AppDbContext db, IImageStorage storage)
        : base(db, storage)
    {
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid areaId, HttpRequest request, CancellationToken ct)
    {
        var exists = await Db.Areas.AsNoTracking().AnyAsync(a => a.Id == areaId, ct);
        if (!exists)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.NotFound();
        }

        var images = await Db.AreaImages.AsNoTracking()
            .Where(i => i.AreaId == areaId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.FileName })
            .ToListAsync(ct);

        var dtos = images
            .Select(i => new ImageDto(i.Id, BuildUrl(request, ImageCategory.Area, areaId, i.FileName), null, "Gallery"))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, HttpRequest request, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult<ImageDto>.Invalid("file", "Image file is required.");
        }

        if (!IsValidImageFile(file))
        {
            return ServiceResult<ImageDto>.Invalid("file", "Only image files are allowed.");
        }

        var exists = await Db.Areas.AsNoTracking().AnyAsync(a => a.Id == areaId, ct);
        if (!exists)
        {
            return ServiceResult<ImageDto>.NotFound();
        }

        var stored = await Storage.SaveAsync(ImageCategory.Area, areaId, file, ct);

        var image = new AreaImage { AreaId = areaId, FileName = stored.FileName, SortOrder = 0 };
        Db.AreaImages.Add(image);
        await Db.SaveChangesAsync(ct);

        var dto = new ImageDto(image.Id, BuildUrl(request, ImageCategory.Area, areaId, image.FileName), null, "Gallery");
        return ServiceResult<ImageDto>.Success(dto);
    }

    public async Task<ServiceResult> DeleteAsync(Guid areaId, Guid imageId, CancellationToken ct)
    {
        var image = await Db.AreaImages.FirstOrDefaultAsync(i => i.Id == imageId && i.AreaId == areaId, ct);
        if (image is null)
        {
            return ServiceResult.NotFound();
        }

        Db.AreaImages.Remove(image);
        await Db.SaveChangesAsync(ct);

        await Storage.DeleteAsync(ImageCategory.Area, areaId, image.FileName, ct);

        return ServiceResult.Success();
    }
}
