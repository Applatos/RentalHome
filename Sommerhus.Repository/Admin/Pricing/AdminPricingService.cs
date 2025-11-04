using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sommerhus.Application.Admin.Pricing;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Abstractions;
using Sommerhus.Pricing.Models;

namespace Sommerhus.Repository.Admin.Pricing;

public sealed class AdminPricingService : IAdminPricingService
{
    private readonly AppDbContext db;
    private readonly IConfiguration configuration;
    private readonly IPricingPipeline pipeline;

    public AdminPricingService(AppDbContext db, IConfiguration configuration, IPricingPipeline pipeline)
    {
        this.db = db;
        this.configuration = configuration;
        this.pipeline = pipeline;
    }

    public async Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto request, CancellationToken ct)
    {
        if (!configuration.GetValue("Pricing:EnabledV1", true))
        {
            return ServiceResult<PriceQuoteResponseDto>.Unavailable("Pricing midlertidigt deaktiveret");
        }

        if (request.Arrival >= request.Departure)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(nameof(request.Departure), "Ugyldigt dato-interval");
        }

        var quote = await pipeline.QuoteAsync(request, ct);
        return ServiceResult<PriceQuoteResponseDto>.Success(quote);
    }

    public async Task<IReadOnlyList<SeasonSpanDto>> GetSeasonSpansAsync(Guid groupId, CancellationToken ct)
    {
        var spans = await db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToListAsync(ct);

        return spans.Select(MapSpan).ToList();
    }

    public async Task<ServiceResult<IReadOnlyList<SeasonSpanDto>>> UpsertSeasonSpansAsync(Guid groupId, IReadOnlyList<SeasonSpanDto> spans, CancellationToken ct)
    {
        var groupExists = await db.HouseGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
        {
            return ServiceResult<IReadOnlyList<SeasonSpanDto>>.NotFound();
        }

        if (spans is null || spans.Count == 0)
        {
            return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Invalid(string.Empty, "Angiv mindst ét span.");
        }

        foreach (var span in spans)
        {
            if (span.EndDate < span.StartDate)
            {
                return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Invalid(nameof(span.EndDate), "EndDate skal være >= StartDate.");
            }

            if (string.IsNullOrWhiteSpace(span.Code))
            {
                return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Invalid(nameof(SeasonSpanDto.Code), "Sæsonkoder er påkrævet for alle spans.");
            }
        }

        var codes = spans
            .Select(s => s.Code!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingCodes = await db.SeasonCodes
            .AsNoTracking()
            .Where(c => codes.Contains(c.Code))
            .Select(c => c.Code)
            .ToListAsync(ct);

        var knownCodes = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        foreach (var code in codes)
        {
            if (!knownCodes.Contains(code))
            {
                return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Invalid(nameof(SeasonSpanDto.Code), $"Ukendt kode: {code}");
            }
        }

        var ordered = spans
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].StartDate <= ordered[i - 1].EndDate)
            {
                return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Invalid(string.Empty, "Spans må ikke overlappe (i samme gruppe).");
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var existing = db.SeasonSpans.Where(s => s.GroupId == groupId);
            db.SeasonSpans.RemoveRange(existing);
            await db.SaveChangesAsync(ct);

            var entities = spans.Select(s => new SeasonSpan
            {
                Id = s.Id == Guid.Empty ? Guid.NewGuid() : s.Id,
                GroupId = groupId,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                Code = s.Code!
            }).ToList();

            await db.SeasonSpans.AddRangeAsync(entities, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            IReadOnlyList<SeasonSpanDto> result = entities.Select(MapSpan).ToList();
            return ServiceResult<IReadOnlyList<SeasonSpanDto>>.Success(result);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<PricePlanDetailsDto>> GetRatePlansAsync(Guid houseId, CancellationToken ct)
    {
        var plans = await db.PricePlans
            .AsNoTracking()
            .Where(p => p.HouseId == houseId)
            .Include(p => p.SeasonPrices)
            .ToListAsync(ct);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<ServiceResult> ActivateRatePlanAsync(Guid planId, CancellationToken ct)
    {
        var plan = await db.PricePlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null)
        {
            return ServiceResult.NotFound();
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.PricePlans
                .Where(p => p.HouseId == plan.HouseId && p.Id != plan.Id && p.IsActive)
                .ExecuteUpdateAsync(up => up
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedUtc, DateTime.UtcNow), ct);

            if (!plan.IsActive)
            {
                plan.IsActive = true;
                plan.UpdatedUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            return ServiceResult.Conflict(nameof(planId), "Kun én aktiv plan pr. hus er tilladt.");
        }
    }

    public async Task<ServiceResult> DeleteRatePlanAsync(Guid houseId, Guid ratePlanId, CancellationToken ct)
    {
        var plan = await db.PricePlans.FirstOrDefaultAsync(p => p.Id == ratePlanId, ct);
        if (plan is null)
        {
            return ServiceResult.NotFound();
        }

        if (plan.HouseId != houseId)
        {
            return ServiceResult.Invalid(nameof(houseId), "Rate plan belongs to another house.");
        }

        db.PricePlans.Remove(plan);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<IReadOnlyList<SeasonCodeDto>> ListSeasonCodesAsync(CancellationToken ct)
    {
        var codes = await db.SeasonCodes
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(ct);

        return codes.Select(c => new SeasonCodeDto(c.Code, c.Name, c.Color, c.SortOrder)).ToList();
    }

    public async Task<ServiceResult<SeasonCodeDto>> CreateSeasonCodeAsync(SeasonCodeDto dto, CancellationToken ct)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Code))
        {
            return ServiceResult<SeasonCodeDto>.Invalid(nameof(dto.Code), "Kode er påkrævet.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();

        var exists = await db.SeasonCodes
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);

        if (exists)
        {
            return ServiceResult<SeasonCodeDto>.Conflict(nameof(dto.Code), $"Sæsonkoden '{code}' findes allerede.");
        }

        string? color = null;
        if (!string.IsNullOrWhiteSpace(dto.Color))
        {
            var normalized = dto.Color.Trim();
            if (!Regex.IsMatch(normalized, "^#?[0-9A-Fa-f]{6}$"))
            {
                return ServiceResult<SeasonCodeDto>.Invalid(nameof(dto.Color), "Farvekoden skal være et hex-format på 6 cifre.");
            }

            color = normalized.StartsWith("#", StringComparison.Ordinal)
                ? normalized.ToUpperInvariant()
                : $"#{normalized.ToUpperInvariant()}";
        }

        var sortOrder = Math.Max(0, dto.SortOrder);

        var entity = new SeasonCode
        {
            Code = code,
            Name = string.IsNullOrWhiteSpace(dto.Label) ? code : dto.Label!.Trim(),
            Color = color ?? "#6C757D",
            SortOrder = sortOrder
        };

        await db.SeasonCodes.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);

        var result = new SeasonCodeDto(entity.Code, entity.Name, entity.Color, entity.SortOrder);
        return ServiceResult<SeasonCodeDto>.Success(result);
    }

    private static SeasonSpanDto MapSpan(SeasonSpan span)
        => new(span.Id, span.GroupId, span.StartDate, span.EndDate, span.Code);

    private static PricePlanDetailsDto MapPlan(PricePlan plan)
        => new(plan.Id, plan.HouseId, plan.Name, plan.Currency, plan.IsActive, plan.CreatedUtc, plan.UpdatedUtc,
            plan.SeasonPrices.Select(sp => new SeasonPriceDto(sp.Id, sp.PricePlanId, sp.Code, sp.NightlyPrice)).ToList());
}
