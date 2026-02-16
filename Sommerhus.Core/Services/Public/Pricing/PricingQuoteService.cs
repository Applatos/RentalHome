using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Availability;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Public.Pricing;

public sealed class PricingQuoteService(
    AppDbContext db,
    IConfiguration configuration,
    IPricingPipeline pipeline,
    IAdminAvailabilityService availabilityService) : IPricingQuoteService
{
    public async Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto request, CancellationToken ct)
    {
        if (!configuration.GetValue("Pricing:EnabledV1", true))
        {
            return ServiceResult<PriceQuoteResponseDto>.Unavailable("Pricing temporarily disabled");
        }

        if (request.Arrival >= request.Departure)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(nameof(request.Departure), "Invalid date range");
        }

        var house = await db.Houses
            .AsNoTracking()
            .Where(h => h.Id == request.HouseId)
            .Select(h => new { h.Id, h.Status })
            .FirstOrDefaultAsync(ct);

        if (house is null)
        {
            return ServiceResult<PriceQuoteResponseDto>.NotFound();
        }

        if (house.Status != EntityStatus.Published)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(nameof(request.HouseId), "House is not available for pricing.");
        }

        var isAvailable = await availabilityService.IsAvailableAsync(request.HouseId, request.Arrival, request.Departure, ct);
        if (!isAvailable)
        {
            return ServiceResult<PriceQuoteResponseDto>.Conflict("dates", "The selected dates are not available.");
        }

        var now = DateTime.UtcNow;
        var cacheRow = await db.Set<PriceQuote>()
            .AsNoTracking()
            .Where(q => q.HouseId == request.HouseId
                        && q.CheckIn == request.Arrival
                        && q.CheckOut == request.Departure
                        && q.Guests == request.Guests
                        && q.ExpiresAtUtc > now)
            .OrderByDescending(q => q.ComputedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (cacheRow is not null)
        {
            var cachedItems = JsonSerializer.Deserialize<List<PriceQuoteLineItemDto>>(cacheRow.NightlyBreakdown) ?? [];
            var cached = new PriceQuoteResponseDto(
                cacheRow.Currency,
                cacheRow.Nights,
                cachedItems,
                cacheRow.Subtotal,
                cacheRow.Tax,
                cacheRow.Total);

            return ServiceResult<PriceQuoteResponseDto>.Success(cached);
        }

        var quote = await pipeline.QuoteAsync(request, ct);

        var ttlMinutes = Math.Clamp(configuration.GetValue("Pricing:QuoteCacheMinutes", 15), 1, 120);

        var newRow = new PriceQuote
        {
            Id = Guid.NewGuid(),
            HouseId = request.HouseId,
            CheckIn = request.Arrival,
            CheckOut = request.Departure,
            Guests = request.Guests,
            Nights = quote.Nights,
            NightlyBreakdown = JsonSerializer.Serialize(quote.Items),
            Modifiers = "[]",
            Subtotal = quote.Subtotal,
            Tax = quote.Tax,
            Total = quote.Total,
            Currency = quote.Currency,
            ComputedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(ttlMinutes)
        };

        db.Set<PriceQuote>().Add(newRow);
        await db.SaveChangesAsync(ct);

        return ServiceResult<PriceQuoteResponseDto>.Success(quote);
    }
}
