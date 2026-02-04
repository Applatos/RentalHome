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
/// Manages images for VacationHouse entities.
/// </summary>
public sealed class AdminHouseImageService : AdminImageServiceBase, IAdminHouseImageService
{
    public AdminHouseImageService(AppDbContext db, IImageStorage storage)
        : base(db, storage)
    {
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid houseId, HttpRequest request, CancellationToken ct)
    {
        var images = await Db.Images.AsNoTracking()
            .Where(i => i.HouseId == houseId)
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : i.Kind == ImageKind.Gallery ? 1 : 2)
            .ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.FileName, i.Alt, i.Kind })
            .ToListAsync(ct);

        if (images.Count == 0)
        {
            var houseExists = await Db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
            if (!houseExists)
            {
                return ServiceResult<IReadOnlyList<ImageDto>>.NotFound();
            }
        }

        var dtos = images
            .Select(i => ToDto(i.Id, request, ImageCategory.House, houseId, i.FileName, i.Alt, i.Kind.ToString()))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid houseId, IFormFileCollection files, HttpRequest request, CancellationToken ct)
    {
        var validationError = ValidateFileCollection(files);
        if (validationError is not null)
            return validationError;

        var exists = await Db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!exists)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.NotFound();
        }

        var added = new List<HouseImage>();
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

            var stored = await SaveToStorageAsync(ImageCategory.House, houseId, file, ct);
            added.Add(new HouseImage
            {
                HouseId = houseId,
                FileName = stored.FileName,
                Kind = ImageKind.Gallery
            });
        }

        if (added.Count == 0)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.Invalid("files", "No valid image files were provided.");
        }

        await Db.Images.AddRangeAsync(added, ct);
        await Db.SaveChangesAsync(ct);

        var dtos = added
            .Select(img => ToDto(img.Id, request, ImageCategory.House, houseId, img.FileName, img.Alt, img.Kind.ToString()))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult> DeleteAsync(Guid houseId, Guid imageId, CancellationToken ct)
    {
        var image = await Db.Images.FirstOrDefaultAsync(i => i.Id == imageId && i.HouseId == houseId, ct);
        if (image is null)
        {
            return ServiceResult.NotFound();
        }

        Db.Images.Remove(image);
        await Db.SaveChangesAsync(ct);

        await DeleteFromStorageAsync(ImageCategory.House, houseId, image.FileName, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetKindAsync(Guid houseId, Guid imageId, ImageKind kind, CancellationToken ct)
    {
        if (!Enum.IsDefined(typeof(ImageKind), kind))
        {
            return ServiceResult.Invalid("kind", "Unknown image kind.");
        }

        var images = await Db.Images.Where(i => i.HouseId == houseId).ToListAsync(ct);
        var target = images.FirstOrDefault(i => i.Id == imageId);
        if (target is null)
        {
            return ServiceResult.NotFound();
        }

        if (kind == ImageKind.Cover)
        {
            ClearExclusiveKind(images, ImageKind.Cover, imageId);
        }
        else if (kind == ImageKind.Floorplan)
        {
            ClearExclusiveKind(images, ImageKind.Floorplan, imageId);
        }

        target.Kind = kind;
        await Db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}
