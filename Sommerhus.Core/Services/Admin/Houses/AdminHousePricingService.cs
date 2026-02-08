using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Admin.Houses;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Admin.Houses;

public sealed class AdminHousePricingService : IAdminHousePricingService
{
    private readonly AppDbContext db;

    public AdminHousePricingService(AppDbContext db)
    {
        this.db = db;
    }

    public async Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
    {
        var house = await db.Houses.AsNoTracking().Include(h => h.Group).FirstOrDefaultAsync(h => h.Id == houseId, ct);
        if (house is null)
        {
            return ServiceResult<PricePlanDetailsDto>.NotFound();
        }

        PricePlan? plan = null;
        if (dto.PlanId != Guid.Empty)
        {
            plan = await db.PricePlans
                .Include(p => p.SeasonPrices)
                .FirstOrDefaultAsync(p => p.HouseId == houseId && p.Id == dto.PlanId, ct);
        }

        plan ??= new PricePlan
        {
            HouseId = houseId
        };

        plan.Name = dto.Name;
        plan.Currency = dto.Currency;
        plan.IsActive = dto.IsActive;
        plan.UpdatedAtUtc = DateTime.UtcNow;

        if (db.Entry(plan).State == EntityState.Detached)
        {
            db.PricePlans.Add(plan);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.SeasonPrices
            .Where(s => s.PricePlanId == plan.Id)
            .ExecuteDeleteAsync(ct);

        if (dto.SeasonPrices.Count > 0)
        {
            var entities = dto.SeasonPrices.Select(s => new SeasonPrice
            {
                Id = Guid.NewGuid(),
                PricePlanId = plan.Id,
                Code = s.Code,
                NightlyPrice = s.NightlyPrice,
            }).ToList();

            await db.SeasonPrices.AddRangeAsync(entities, ct);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var refreshed = await db.PricePlans
            .AsNoTracking()
            .Include(p => p.SeasonPrices)
            .FirstAsync(p => p.Id == plan.Id, ct);

        return ServiceResult<PricePlanDetailsDto>.Success(PricePlanMapper.ToDto(refreshed));
    }
}
