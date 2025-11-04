using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.Features;
using Sommerhus.Application.Common;
using Sommerhus.Application.Storage;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Domain.Models;
using Sommerhus.Repository;

namespace Sommerhus.Repository.Admin.Features;

public sealed class AdminFeatureService : IAdminFeatureService
{
    private readonly AppDbContext db;
    private readonly IImageStorage imageStorage;

    public AdminFeatureService(AppDbContext db, IImageStorage imageStorage)
    {
        this.db = db;
        this.imageStorage = imageStorage;
    }

    public async Task<IReadOnlyList<FeatureDetailsDto>> GetAllAsync(HttpRequest request, CancellationToken ct)
    {
        var items = await db.Features.AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

        return items
            .Select(f => new FeatureDetailsDto(
                f.Id,
                f.Name,
                f.Key,
                f.ValueType.ToString(),
                f.Unit,
                imageStorage.GetUrl(request, f.IconUrl)))
            .ToList();
    }

    public async Task<ServiceResult<Guid>> CreateAsync(UpsertFeatureDto dto, CancellationToken ct)
    {
        var valueTypeResult = NormalizeValueType(dto.ValueType);
        if (!valueTypeResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(CloneErrors(valueTypeResult.Errors));
        }

        var keyResult = NormalizeKey(dto.Key);
        if (!keyResult.IsSuccess)
        {
            return ServiceResult<Guid>.Invalid(CloneErrors(keyResult.Errors));
        }

        var exists = await db.Features.AnyAsync(f => f.Key == keyResult.Value, ct);
        if (exists)
        {
            return ServiceResult<Guid>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key er allerede i brug." }
            });
        }

        var feature = new Feature
        {
            Name = NormalizeName(dto.Name),
            Key = keyResult.Value!,
            ValueType = valueTypeResult.Value,
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
            return ServiceResult.Invalid(CloneErrors(valueTypeResult.Errors));
        }

        var keyResult = NormalizeKey(dto.Key);
        if (!keyResult.IsSuccess)
        {
            return ServiceResult.Invalid(CloneErrors(keyResult.Errors));
        }

        var keyTaken = await db.Features.AnyAsync(f => f.Key == keyResult.Value && f.Id != id, ct);
        if (keyTaken)
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key er allerede i brug." }
            });
        }

        feature.Name = NormalizeName(dto.Name);
        feature.Key = keyResult.Value!;
        feature.ValueType = valueTypeResult.Value;
        feature.Unit = NormalizeUnit(dto.Unit);

        await db.SaveChangesAsync(ct);
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
            return ServiceResult.Conflict(nameof(id), "Feature er knyttet til et eller flere huse og kan ikke slettes.");
        }

        db.Features.Remove(feature);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Conflict(nameof(id), "Feature er knyttet til et eller flere huse og kan ikke slettes.");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<string>> UploadIconAsync(Guid id, IFormFile file, HttpRequest request, CancellationToken ct)
    {
        var feature = await db.Features.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (feature is null)
        {
            return ServiceResult<string>.NotFound();
        }

        var fileResult = ValidateIcon(file);
        if (!fileResult.IsSuccess)
        {
            return ServiceResult<string>.Invalid(CloneErrors(fileResult.Errors));
        }

        await imageStorage.DeleteAsync(feature.IconUrl, ct);

        var stored = await imageStorage.SaveAsync(ImageCategory.Feature, id, file, ct);
        feature.IconUrl = stored.RelativePath;
        await db.SaveChangesAsync(ct);

        var absolute = imageStorage.GetUrl(request, feature.IconUrl);
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

    private static ServiceResult<FeatureValueType> NormalizeValueType(string? valueType)
    {
        if (Enum.TryParse<FeatureValueType>(valueType, true, out var parsed))
        {
            return ServiceResult<FeatureValueType>.Success(parsed);
        }

        return ServiceResult<FeatureValueType>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(UpsertFeatureDto.ValueType)] = new[] { "Ugyldig ValueType (tilladt: Bool, Int, Decimal, Text)." }
        });
    }

    private static ServiceResult<string> NormalizeKey(string? key)
    {
        var trimmed = (key ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > 60 || !trimmed.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-'))
        {
            return ServiceResult<string>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(UpsertFeatureDto.Key)] = new[] { "Key skal være [a-zA-Z0-9_-], maks 60 tegn." }
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
                ["file"] = new[] { "Ingen fil modtaget." }
            });
        }

        if (file.Length > 2 * 1024 * 1024)
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = new[] { "Fil er for stor (maks 2MB)." }
            });
        }

        var allowed = new[] { "image/png", "image/jpeg" };
        if (file.ContentType is null || !allowed.Contains(file.ContentType))
        {
            return ServiceResult.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["file"] = new[] { "Kun PNG og JPEG er tilladt." }
            });
        }

        return ServiceResult.Success();
    }

    private static string NormalizeName(string? name)
        => (name ?? string.Empty).Trim();

    private static string? NormalizeUnit(string? unit)
    {
        var trimmed = (unit ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static Dictionary<string, string[]> CloneErrors(IReadOnlyDictionary<string, string[]> errors)
    {
        var copy = new Dictionary<string, string[]>(errors.Count, StringComparer.Ordinal);
        foreach (var pair in errors)
        {
            copy[pair.Key] = pair.Value.ToArray();
        }

        return copy;
    }
}
