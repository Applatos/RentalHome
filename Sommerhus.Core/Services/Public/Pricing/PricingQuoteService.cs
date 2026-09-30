using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Availability;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Services.Pricing.Engine.Rules;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Services.Public.Pricing;

public sealed class PricingQuoteService(
    AppDbContext db,
    IConfiguration configuration,
    IPricingPipeline pipeline,
    IAdminAvailabilityService availabilityService) : IPricingQuoteService
{
    /// <summary>
    /// The longest stay a quote accepts.
    /// </summary>
    public const int MaxNights = 365;

    private static readonly JsonSerializerOptions CacheJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto request, CancellationToken ct)
    {
        if (!configuration.GetValue("Pricing:EnabledV1", true))
        {
            return ServiceResult<PriceQuoteResponseDto>.Unavailable("Pricing temporarily disabled");
        }

        if (request.Arrival >= request.Departure)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.Dates, "Departure must be after arrival.");
        }

        if (request.Arrival < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.Dates, "Arrival cannot be in the past.");
        }

        if (request.Departure.DayNumber - request.Arrival.DayNumber > MaxNights)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.Dates, $"A stay can be at most {MaxNights} nights.");
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

        var guestsError = await ValidateGuestsAsync(request, ct);
        if (guestsError is not null)
        {
            return guestsError;
        }

        var isAvailable = await availabilityService.IsAvailableAsync(request.HouseId, request.Arrival, request.Departure, ct);
        if (!isAvailable)
        {
            return ServiceResult<PriceQuoteResponseDto>.Conflict(PricingErrors.Dates, "The selected dates are not available.");
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
                cacheRow.Total,
                VatRule.IncludedIn(cacheRow.Total, VatRule.ReadRate(configuration)));

            return ServiceResult<PriceQuoteResponseDto>.Success(cached);
        }

        var result = await pipeline.QuoteAsync(request, ct);

        // A failed quote (e.g. unpriced nights) is never cached.
        if (!result.IsSuccess || result.Value is null)
        {
            return result;
        }

        var quote = result.Value;
        var ttlMinutes = Math.Clamp(configuration.GetValue("Pricing:QuoteCacheMinutes", 15), 1, 120);

        var newRow = new PriceQuote
        {
            Id = Guid.NewGuid(),
            HouseId = request.HouseId,
            CheckIn = request.Arrival,
            CheckOut = request.Departure,
            Guests = request.Guests,
            Nights = quote.Nights,
            NightlyBreakdown = JsonSerializer.Serialize(quote.Items, CacheJsonOptions),
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

    private async Task<ServiceResult<PriceQuoteResponseDto>?> ValidateGuestsAsync(PriceQuoteRequestDto request, CancellationToken ct)
    {
        if (request.Guests < 1)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.Guests, "At least one guest is required.");
        }

        var capacityFeatures = await db.HouseFeatures
            .AsNoTracking()
            .Include(hf => hf.Feature)
            .Where(hf => hf.HouseId == request.HouseId
                         && hf.Feature != null
                         && HouseCapacity.FeatureKeys.Contains(hf.Feature.Key.ToLower()))
            .ToListAsync(ct);

        var capacity = HouseCapacity.MaxGuests(capacityFeatures);
        if (capacity is not null && request.Guests > capacity)
        {
            return ServiceResult<PriceQuoteResponseDto>.Invalid(PricingErrors.Guests, $"This house sleeps at most {capacity} guests.");
        }

        return null;
    }
}
