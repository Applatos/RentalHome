using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Houses;

public sealed class AdminHousePricingService(
    AppDbContext db,
    IPriceSummaryService priceSummaryService) : IAdminHousePricingService
{
    private const string SeasonPricesField = "seasonPrices";

    public async Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
    {
        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
        {
            return ServiceResult<PricePlanDetailsDto>.NotFound();
        }

        var pricesResult = await ValidateSeasonPricesAsync(dto.SeasonPrices, ct);
        if (!pricesResult.IsSuccess)
        {
            return ServiceResult<PricePlanDetailsDto>.FailureFrom(pricesResult);
        }

        // Saving updates the plan the form shows instead of adding another one, so a house that
        // is edited through the form keeps a single plan.
        var plan = await FindPlanAsync(houseId, dto.PlanId, ct)
            ?? new PricePlan { HouseId = houseId };

        plan.Name = string.IsNullOrWhiteSpace(dto.Name) ? "Standard" : dto.Name.Trim();
        plan.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "DKK" : dto.Currency.Trim().ToUpperInvariant();
        plan.IsActive = dto.IsActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        if (db.Entry(plan).State == EntityState.Detached)
        {
            db.PricePlans.Add(plan);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (plan.IsActive)
        {
            // At most one active plan per house.
            await db.PricePlans
                .Where(p => p.HouseId == houseId && p.Id != plan.Id && p.IsActive)
                .ExecuteUpdateAsync(up => up
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedAtUtc, DateTime.UtcNow), ct);
        }

        await db.SeasonPrices
            .Where(s => s.PricePlanId == plan.Id)
            .ExecuteDeleteAsync(ct);

        var entities = pricesResult.Value!.Select(s => new SeasonPrice
        {
            Id = Guid.NewGuid(),
            PricePlanId = plan.Id,
            Code = s.Code,
            NightlyPrice = s.NightlyPrice,
        }).ToList();

        await db.SeasonPrices.AddRangeAsync(entities, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await priceSummaryService.RecomputeSummaryAsync(houseId, ct);

        var refreshed = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .FirstAsync(p => p.Id == plan.Id, ct);

        return ServiceResult<PricePlanDetailsDto>.Success(PricePlanMapper.ToDto(refreshed));
    }

    /// <summary>
    /// The requested plan when it belongs to the house, otherwise the plan the form edits.
    /// </summary>
    private async Task<PricePlan?> FindPlanAsync(Guid houseId, Guid planId, CancellationToken ct)
    {
        if (planId != Guid.Empty)
        {
            var requested = await db.PricePlans.FirstOrDefaultAsync(p => p.HouseId == houseId && p.Id == planId, ct);
            if (requested is not null)
            {
                return requested;
            }
        }

        return await db.PricePlans.ForEditing(houseId).FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Normalises the codes (trimmed, upper-case) and rejects empty, duplicate and unknown codes
    /// and prices that are not above zero, reporting every problem at once.
    /// </summary>
    private async Task<ServiceResult<IReadOnlyList<NormalizedSeasonPrice>>> ValidateSeasonPricesAsync(
        IReadOnlyList<SeasonPriceDto>? prices,
        CancellationToken ct)
    {
        var errors = new List<string>();
        var normalized = new List<NormalizedSeasonPrice>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var price in prices ?? [])
        {
            var code = SeasonCodeFormat.Normalize(price.Code);
            if (code.Length == 0)
            {
                errors.Add("Every season price needs a season code.");
                continue;
            }

            if (!seen.Add(code))
            {
                errors.Add($"Season code '{code}' is listed more than once.");
                continue;
            }

            if (price.NightlyPrice <= 0)
            {
                errors.Add($"The nightly price for season '{code}' must be greater than zero.");
            }

            normalized.Add(new NormalizedSeasonPrice(code, price.NightlyPrice));
        }

        if (seen.Count > 0)
        {
            var codes = seen.ToList();
            var known = await db.SeasonCodes
                .AsNoTracking()
                .Where(c => codes.Contains(c.Code))
                .Select(c => c.Code)
                .ToListAsync(ct);

            errors.AddRange(codes
                .Except(known, StringComparer.Ordinal)
                .Select(code => $"Unknown season code: {code}"));
        }

        if (errors.Count > 0)
        {
            return ServiceResult<IReadOnlyList<NormalizedSeasonPrice>>.Invalid(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [SeasonPricesField] = errors.ToArray()
            });
        }

        return ServiceResult<IReadOnlyList<NormalizedSeasonPrice>>.Success(normalized);
    }

    private sealed record NormalizedSeasonPrice(string Code, decimal NightlyPrice);
}
