using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Houses;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Houses;

public sealed class AdminHouseFeatureService(AppDbContext db) : IAdminHouseFeatureService
{

    public async Task<ServiceResult> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct)
    {
        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
        {
            return ServiceResult.NotFound();
        }

        var normalized = FeatureValueNormalizer.Normalize(houseId, values);

        var featureIds = normalized.Select(i => i.FeatureId).Distinct().ToList();
        if (featureIds.Count > 0)
        {
            var existingFeatureIds = await db.Features
                .AsNoTracking()
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(ct);

            var missing = featureIds.Except(existingFeatureIds).ToList();
            if (missing.Count > 0)
            {
                return ServiceResult.Invalid("FeatureIds", $"Unknown feature IDs: {string.Join(", ", missing)}");
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.HouseFeatures.Where(hf => hf.HouseId == houseId).ExecuteDeleteAsync(ct);

        if (normalized.Count > 0)
        {
            await db.HouseFeatures.AddRangeAsync(normalized, ct);
            await db.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);
        return ServiceResult.Success();
    }
}
