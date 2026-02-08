using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Images;

/// <summary>
/// Manages images for Area entities.
/// </summary>
public sealed class AdminAreaImageService : AdminImageServiceBase, IAdminAreaImageService
{
    public AdminAreaImageService(AppDbContext db, IImageStorage storage)
        : base(db, storage)
    {
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid areaId, string baseUrl, CancellationToken ct)
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
            .Select(i => ToDto(i.Id, baseUrl, ImageCategory.Area, areaId, i.FileName, null, "Gallery"))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, string baseUrl, CancellationToken ct)
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

        return ServiceResult<ImageDto>.Success(ToDto(image.Id, baseUrl, ImageCategory.Area, areaId, image.FileName, null, "Gallery"));
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid areaId, IFormFileCollection files, string baseUrl, CancellationToken ct)
    {
        var validationError = ValidateFileCollection(files);
        if (validationError is not null)
            return validationError;

        var exists = await Db.Areas.AsNoTracking().AnyAsync(a => a.Id == areaId, ct);
        if (!exists)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.NotFound();
        }

        var added = new List<AreaImage>();
        foreach (var file in files)
        {
            if (file is null || file.Length == 0)
            {
                continue;
            }

            if (!IsValidImageFile(file))
            {
                return ServiceResult<IReadOnlyList<ImageDto>>.Invalid("files", "Only image files are allowed.");
            }

            var stored = await SaveToStorageAsync(ImageCategory.Area, areaId, file, ct);
            added.Add(new AreaImage
            {
                AreaId = areaId,
                FileName = stored.FileName,
                SortOrder = 0
            });
        }

        if (added.Count == 0)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.Invalid("files", "No valid image files were provided.");
        }

        Db.AreaImages.AddRange(added);
        await Db.SaveChangesAsync(ct);

        var dtos = added
            .Select(img => ToDto(img.Id, baseUrl, ImageCategory.Area, areaId, img.FileName, null, "Gallery"))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
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
