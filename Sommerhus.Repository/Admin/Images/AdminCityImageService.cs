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

public sealed class AdminCityImageService : AdminImageServiceBase, IAdminCityImageService
{
    public AdminCityImageService(AppDbContext db, IImageStorage storage)
        : base(db, storage)
    {
    }

    public async Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid cityId, HttpRequest request, CancellationToken ct)
    {
        var city = await Db.Cities
            .Include(c => c.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cityId, ct);

        if (city is null)
        {
            return ServiceResult<IReadOnlyList<ImageDto>>.NotFound();
        }

        var dtos = city.Images
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(i.Id, BuildUrl(request, ImageCategory.City, city.Id, i.FileName), i.Alt, "city"))
            .ToList();

        return ServiceResult<IReadOnlyList<ImageDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ImageDto>> UploadAsync(Guid cityId, IFormFile file, string? alt, HttpRequest request, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult<ImageDto>.Invalid("file", "Image file is required.");
        }

        if (!IsValidImageFile(file))
        {
            return ServiceResult<ImageDto>.Invalid("file", "Only image files are allowed.");
        }

        var city = await Db.Cities.Include(c => c.Images).FirstOrDefaultAsync(c => c.Id == cityId, ct);
        if (city is null)
        {
            return ServiceResult<ImageDto>.NotFound();
        }

        var stored = await Storage.SaveAsync(ImageCategory.City, cityId, file, ct);

        var sortOrder = city.Images.Count == 0 ? 0 : city.Images.Max(i => i.SortOrder) + 10;
        var image = new CityImage
        {
            CityId = cityId,
            FileName = stored.FileName,
            Alt = NormalizeAlt(alt),
            SortOrder = sortOrder
        };

        Db.CityImages.Add(image);
        await Db.SaveChangesAsync(ct);

        var dto = new ImageDto(image.Id, BuildUrl(request, ImageCategory.City, cityId, image.FileName), image.Alt, "city");
        return ServiceResult<ImageDto>.Success(dto);
    }

    public async Task<ServiceResult> DeleteAsync(Guid cityId, Guid imageId, CancellationToken ct)
    {
        var image = await Db.CityImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CityId == cityId, ct);
        if (image is null)
        {
            return ServiceResult.NotFound();
        }

        Db.CityImages.Remove(image);
        await Db.SaveChangesAsync(ct);

        await Storage.DeleteAsync(ImageCategory.City, cityId, image.FileName, ct);
        return ServiceResult.Success();
    }
}
