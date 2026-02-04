using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Areas;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Domain.Models;
using Sommerhus.Core.Dtos.Admin.Areas;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Areas;

public sealed class AdminAreaService : IAdminAreaService
{
    private readonly AppDbContext db;
    private readonly IImageStorage imageStorage;

    public AdminAreaService(AppDbContext db, IImageStorage imageStorage)
    {
        this.db = db;
        this.imageStorage = imageStorage;
    }

    public async Task<IReadOnlyList<AreaListItemDto>> GetAllAsync(CancellationToken ct)
        => await db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AreaListItemDto(a.Id, a.Name, a.Houses.Count))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LookupItem>> GetLookupAsync(CancellationToken ct)
        => await db.Areas.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToListAsync(ct);

    public async Task<ServiceResult<AreaDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct)
    {
        var area = await LoadAreaAsync(id, ct);
        if (area is null)
        {
            return ServiceResult<AreaDetailsDto>.NotFound();
        }

        return ServiceResult<AreaDetailsDto>.Success(MapDetails(area, request));
    }

    public async Task<ServiceResult<AreaDetailsDto>> CreateAsync(UpsertAreaDto dto, HttpRequest request, CancellationToken ct)
    {
        var nameResult = NormalizeName(dto.Name);
        if (!nameResult.IsSuccess)
        {
            return ServiceResult<AreaDetailsDto>.Invalid(CloneErrors(nameResult.Errors));
        }

        var citiesResult = await ResolveCitiesAsync(dto.CityIds, ct);
        if (!citiesResult.IsSuccess)
        {
            return ServiceResult<AreaDetailsDto>.Invalid(CloneErrors(citiesResult.Errors));
        }

        var area = new Area
        {
            Name = nameResult.Value!,
            Description = NormalizeDescription(dto.Description)
        };

        foreach (var city in citiesResult.Value ?? Array.Empty<City>())
        {
            area.Cities.Add(city);
        }

        var images = NormalizeImages(dto.Images).ToList();
        if (images.Count > 0)
        {
            foreach (var img in images)
            {
                img.AreaId = area.Id;
            }

            area.AreaImages = images;
        }

        db.Areas.Add(area);
        await db.SaveChangesAsync(ct);

        var created = await LoadAreaAsync(area.Id, ct);
        return ServiceResult<AreaDetailsDto>.Success(MapDetails(created!, request));
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpsertAreaDto dto, CancellationToken ct)
    {
        var area = await db.Areas
            .Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null)
        {
            return ServiceResult.NotFound();
        }

        var nameResult = NormalizeName(dto.Name);
        if (!nameResult.IsSuccess)
        {
            return ServiceResult.Invalid(CloneErrors(nameResult.Errors));
        }

        var citiesResult = await ResolveCitiesAsync(dto.CityIds, ct);
        if (!citiesResult.IsSuccess)
        {
            return ServiceResult.Invalid(CloneErrors(citiesResult.Errors));
        }

        area.Name = nameResult.Value!;
        area.Description = NormalizeDescription(dto.Description);

        area.Cities.Clear();
        foreach (var city in citiesResult.Value ?? Array.Empty<City>())
        {
            area.Cities.Add(city);
        }

        if (dto.Images is not null)
        {
            db.AreaImages.RemoveRange(area.AreaImages);
            var updatedImages = NormalizeImages(dto.Images).ToList();
            foreach (var img in updatedImages)
            {
                img.AreaId = area.Id;
            }

            area.AreaImages = updatedImages;
        }

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var area = await db.Areas
            .Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (area is null)
        {
            return ServiceResult.NotFound();
        }

        db.Areas.Remove(area);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    private async Task<Area?> LoadAreaAsync(Guid id, CancellationToken ct)
        => await db.Areas
            .Include(a => a.AreaImages)
            .Include(a => a.Cities)
            .Include(a => a.Houses)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    private static ServiceResult<string> NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return ServiceResult<string>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertAreaDto.Name)] = new[] { "Navn er påkrævet" }
            });
        }

        return ServiceResult<string>.Success(trimmed);
    }

    private async Task<ServiceResult<IReadOnlyList<City>>> ResolveCitiesAsync(IEnumerable<Guid>? cityIds, CancellationToken ct)
    {
        if (cityIds is null)
        {
            return ServiceResult<IReadOnlyList<City>>.Success(Array.Empty<City>());
        }

        var ids = cityIds.ToList();
        if (ids.Any(id => id == Guid.Empty))
        {
            return InvalidCityIds();
        }

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (normalizedIds.Count == 0)
        {
            return ServiceResult<IReadOnlyList<City>>.Success(Array.Empty<City>());
        }

        var cities = await db.Cities.Where(c => normalizedIds.Contains(c.Id)).ToListAsync(ct);
        if (cities.Count != normalizedIds.Count)
        {
            return InvalidCityIds();
        }

        return ServiceResult<IReadOnlyList<City>>.Success(cities);
    }

    private static ServiceResult<IReadOnlyList<City>> InvalidCityIds()
        => ServiceResult<IReadOnlyList<City>>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(UpsertAreaDto.CityIds)] = new[] { "Ukendt by" }
        });

    private static string? NormalizeDescription(string? description)
        => string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    private static IEnumerable<AreaImage> NormalizeImages(IReadOnlyList<string>? images)
    {
        if (images is null || images.Count == 0)
        {
            yield break;
        }

        var order = 0;
        foreach (var img in images)
        {
            if (string.IsNullOrWhiteSpace(img))
            {
                continue;
            }

            yield return new AreaImage
            {
                FileName = img.Trim(),
                SortOrder = order++
            };
        }
    }

    private AreaDetailsDto MapDetails(Area area, HttpRequest request)
    {
        var houses = area.Houses
            .OrderBy(h => h.Title)
            .ThenBy(h => h.Id)
            .Select(h => new AreaHouseDto(h.Id, h.Title))
            .ToList();

        var images = area.AreaImages
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Select(i => new ImageDto(
                i.Id,
                imageStorage.GetUrl(request, ImageCategory.Area, area.Id, i.FileName),
                null,
                "Gallery"))
            .ToList();

        var cityItems = area.Cities
            .OrderBy(c => c.Zip)
            .ThenBy(c => c.Name)
            .Select(c => new LookupItem(c.Id, $"{c.Zip}  {c.Name}"))
            .ToList();

        var cityIds = cityItems.Select(c => c.Id).ToList();

        return new AreaDetailsDto(area.Id, area.Name, cityIds, cityItems, area.Description, images, houses);
    }

    private static Dictionary<string, string[]> CloneErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            dict[pair.Key] = pair.Value?.ToArray() ?? Array.Empty<string>();
        }

        return dict;
    }
}
