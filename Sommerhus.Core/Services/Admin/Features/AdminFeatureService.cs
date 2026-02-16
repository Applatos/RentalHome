using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Core;
using Sommerhus.Core.Services.Public.Houses;

namespace Sommerhus.Core.Services.Admin.Features;

public sealed class AdminFeatureService(
    AppDbContext db,
    IImageStorage imageStorage,
    ISearchIndexer searchIndexer) : IAdminFeatureService
{

    public async Task<IReadOnlyList<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct)
    {
        var items = await db.Features.AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

        return items
            .Select(f => new FeatureDto(
                f.Id,
                f.Name,
                f.Key,
                f.ValueType,
                f.Category,
                f.IsSearchable,
                f.Options,
                f.Unit,
                imageStorage.GetUrl(baseUrl, f.IconUrl)))
            .ToList();
    }

    public async Task<ServiceResult<Guid>> CreateAsync(UpsertFeatureDto dto, CancellationToken ct)
    {
        var valueTypeResult = NormalizeValueType(dto.ValueType);
        if (!valueTypeResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(valueTypeResult.Errors);
        }

        var keyResult = NormalizeKey(dto.Key);
        if (!keyResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(keyResult.Errors);
        }

        var exists = await db.Features.AnyAsync(f => f.Key == keyResult.Value, ct);
        if (exists)
        {
            return ServiceResult<Guid>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key is already in use." }
            });
        }

        var categoryResult = NormalizeCategory(dto.Category);

        var feature = new Feature
        {
            Name = NormalizeName(dto.Name),
            NameEn = NormalizeOptionalName(dto.NameEn),
            Key = keyResult.Value!,
            ValueType = valueTypeResult.Value,
            Category = categoryResult,
            IsSearchable = dto.IsSearchable,
            Options = NormalizeOptions(dto.Options),
            Unit = NormalizeUnit(dto.Unit),
        };

        db.Features.Add(feature);
        await db.SaveChangesAsync(ct);

        return ServiceResult<Guid>.Success(feature.Id);
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpsertFeatureDto dto, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return ServiceResult.NotFound();
        }

        var valueTypeResult = NormalizeValueType(dto.ValueType);
        if (!valueTypeResult.IsSuccess)
        {
            return ServiceResult.Invalid(valueTypeResult.Errors);
        }

        var keyResult = NormalizeKey(dto.Key);
        if (!keyResult.IsSuccess)
        {
            return ServiceResult.Invalid(keyResult.Errors);
        }

        var keyTaken = await db.Features.AnyAsync(f => f.Key == keyResult.Value && f.Id != id, ct);
        if (keyTaken)
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key is already in use." }
            });
        }

        var categoryResult = NormalizeCategory(dto.Category);

        feature.Name = NormalizeName(dto.Name);
        feature.NameEn = NormalizeOptionalName(dto.NameEn);
        feature.Key = keyResult.Value!;
        feature.ValueType = valueTypeResult.Value;
        feature.Category = categoryResult;
        feature.IsSearchable = dto.IsSearchable;
        feature.Options = NormalizeOptions(dto.Options);
        feature.Unit = NormalizeUnit(dto.Unit);

        await db.SaveChangesAsync(ct);
        await ReindexFeatureHousesAsync(feature.Id, ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return ServiceResult.NotFound();
        }

        var inUse = await db.HouseFeatures.AsNoTracking().AnyAsync(v => v.FeatureId == id, ct);
        if (inUse)
        {
            return ServiceResult.Conflict(nameof(id), "Feature is linked to one or more houses and cannot be deleted.");
        }

        db.Features.Remove(feature);
        try
        {
            await db.SaveChangesAsync(ct);
            await ReindexFeatureHousesAsync(id, ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Conflict(nameof(id), "Feature is linked to one or more houses and cannot be deleted.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<string>> UploadIconAsync(Guid id, IFormFile file, string baseUrl, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return ServiceResult<string>.NotFound();
        }

        var fileResult = ValidateIcon(file);
        if (!fileResult.IsSuccess)
        {
            return ServiceResult<string>.Invalid(fileResult.Errors);
        }

        await imageStorage.DeleteAsync(feature.IconUrl, ct);

        var stored = await imageStorage.SaveAsync(ImageCategory.Feature, id, file, ct);
        feature.IconUrl = stored.RelativePath;
        await db.SaveChangesAsync(ct);

        var absolute = imageStorage.GetUrl(baseUrl, feature.IconUrl);
        return ServiceResult<string>.Success(absolute!);
    }

    public async Task<ServiceResult> DeleteIconAsync(Guid id, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return ServiceResult.NotFound();
        }

        await imageStorage.DeleteAsync(feature.IconUrl, ct);
        feature.IconUrl = null;
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    private async Task ReindexFeatureHousesAsync(Guid featureId, CancellationToken ct)
    {
        var houseIds = await db.HouseFeatures
            .AsNoTracking()
            .Where(hf => hf.FeatureId == featureId)
            .Select(hf => hf.HouseId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var houseId in houseIds)
        {
            await searchIndexer.UpdateHouseAsync(houseId, ct);
        }
    }

    private static ServiceResult<FeatureValueType> NormalizeValueType(string? valueType)
    {
        if (Enum.TryParse<FeatureValueType>(valueType, true, out var parsed))
        {
            return ServiceResult<FeatureValueType>.Success(parsed);
        }

        return ServiceResult<FeatureValueType>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(UpsertFeatureDto.ValueType)] = new[] { "Invalid ValueType (allowed: Bool, Int, Decimal, Text)." }
        });
    }

    private static ServiceResult<string> NormalizeKey(string? key)
    {
        var trimmed = (key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 60 || !trimmed.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
        {
            return ServiceResult<string>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key must match [a-zA-Z0-9_-], max 60 characters." }
            });
        }

        return ServiceResult<string>.Success(trimmed);
    }

    private static ServiceResult ValidateIcon(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = new[] { "No file received." }
            });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = new[] { "File is too large (max 2MB)." }
            });
        }

        var allowed = new[] { "image/png", "image/jpeg" };
        if (file.ContentType is null || !allowed.Contains(file.ContentType))
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = new[] { "Only PNG and JPEG are allowed." }
            });
        }

        return ServiceResult.Success();
    }

    private static string NormalizeName(string? name)
        => (name ?? string.Empty).Trim();

    private static string? NormalizeOptionalName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeUnit(string? unit)
    {
        var trimmed = (unit ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static FeatureCategory NormalizeCategory(string? category)
    {
        if (Enum.TryParse<FeatureCategory>(category, true, out var parsed))
            return parsed;
        return FeatureCategory.Other;
    }

    private static string? NormalizeOptions(string? options)
    {
        var trimmed = (options ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
