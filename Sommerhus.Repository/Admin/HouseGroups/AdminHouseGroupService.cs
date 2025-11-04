using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Repository;

namespace Sommerhus.Repository.Admin.HouseGroups;

public sealed class AdminHouseGroupService : IAdminHouseGroupService
{
    private readonly AppDbContext db;

    public AdminHouseGroupService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<LookupItem>> GetAllAsync(CancellationToken ct)
        => await db.HouseGroups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new LookupItem(g.Id, g.Name))
            .ToListAsync(ct);

    public async Task<ServiceResult<LookupItem>> CreateAsync(HouseGroupDto dto, CancellationToken ct)
    {
        var nameResult = NormalizeName(dto?.name);
        if (!nameResult.IsSuccess)
        {
            return ServiceResult<LookupItem>.Invalid(CloneErrors(nameResult.Errors));
        }

        var normalizedName = nameResult.Value!;
        var exists = await db.HouseGroups.AsNoTracking().AnyAsync(g => g.Name == normalizedName, ct);
        if (exists)
        {
            return ServiceResult<LookupItem>.Conflict(nameof(HouseGroupDto.name), "En gruppe med dette navn findes allerede.");
        }

        var entity = new HouseGroup
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
        };

        await db.HouseGroups.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);

        return ServiceResult<LookupItem>.Success(new LookupItem(entity.Id, entity.Name));
    }

    private static ServiceResult<string> NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return ServiceResult<string>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(HouseGroupDto.name)] = new[] { "Navn er påkrævet." }
            });
        }

        return ServiceResult<string>.Success(trimmed);
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
