namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Price quote request DTO.
/// </summary>
public record PriceQuoteRequestDto(Guid HouseId, DateOnly Arrival, DateOnly Departure, int Guests, Guid? AreaId);

/// <summary>
/// Price quote line item DTO.
/// </summary>
/// <remarks>
/// <see cref="Text"/> is an English fallback. Clients render their own label from the
/// structured fields: BASE lines carry <see cref="Nights"/>, <see cref="UnitPrice"/>,
/// <see cref="SeasonCode"/> and <see cref="SeasonName"/>; GUEST lines carry
/// <see cref="Nights"/>, <see cref="Guests"/> (extra guests) and <see cref="UnitPrice"/>;
/// CLEAN lines carry only the amount.
/// </remarks>
public record PriceQuoteLineItemDto(
    string Code,
    string Text,
    decimal Amount,
    int? Nights = null,
    int? Guests = null,
    decimal? UnitPrice = null,
    string? SeasonCode = null,
    string? SeasonName = null);

/// <summary>
/// Price quote response DTO. Prices include VAT; <see cref="VatIncluded"/> is the VAT share of
/// <see cref="Total"/>, and <see cref="Tax"/> is VAT added on top, which is zero.
/// </summary>
public record PriceQuoteResponseDto(
    string Currency,
    int Nights,
    IReadOnlyList<PriceQuoteLineItemDto> Items,
    decimal Subtotal,
    decimal Tax,
    decimal Total,
    decimal VatIncluded = 0);

/// <summary>
/// Error keys a quote or booking can fail with. They are the field names of the validation
/// problem the API returns, so a client can show its own text for each.
/// </summary>
public static class PricingErrors
{
    /// <summary>One or more nights of the stay have no season price.</summary>
    public const string UnpricedNights = "unpricedNights";

    /// <summary>The guest count is below one or above the house's capacity.</summary>
    public const string Guests = "guests";

    /// <summary>The dates are invalid or not available.</summary>
    public const string Dates = "dates";
}
