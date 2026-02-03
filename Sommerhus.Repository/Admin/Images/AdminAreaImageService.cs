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

/// <summary>
/// Manages images for Area entities.
/// </summary>
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
            .Select(i => ToDto(i.Id, request, ImageCategory.Area, areaId, i.FileName, null, "Gallery"))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, HttpRequest request, CancellationToken ct)
    {
        var validationError = ValidateSingleFile<ImageDto>(file);
        if (validationError is not null)
            return validationError;

        var exists = await Db.Areas.AsNoTracking().AnyAsync(a => a.Id == areaId, ct);
        if (!exists)
        {
            return ServiceResult<ImageDto>.NotFound();
        }

        var stored = await SaveToStorageAsync(ImageCategory.Area, areaId, file!, ct);

        var image = new AreaImage { AreaId = areaId, FileName = stored.FileName, SortOrder = 0 };
        Db.AreaImages.Add(image);
        await Db.SaveChangesAsync(ct);

        return ServiceResult<ImageDto>.Success(ToDto(image.Id, request, ImageCategory.Area, areaId, image.FileName, null, "Gallery"));
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

        await DeleteFromStorageAsync(ImageCategory.Area, areaId, image.FileName, ct);
        return ServiceResult.Success();
    }
}
