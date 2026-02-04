namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Price quote request DTO.
/// </summary>
public record PriceQuoteRequestDto(Guid HouseId, DateOnly Arrival, DateOnly Departure, int Guests, Guid? AreaId);

/// <summary>
/// Price quote line item DTO.
/// </summary>
public record PriceQuoteLineItemDto(string Code, string Text, decimal Amount);

/// <summary>
/// Price quote response DTO.
/// </summary>
public record PriceQuoteResponseDto(string Currency, int Nights, IReadOnlyList<PriceQuoteLineItemDto> Items, decimal Subtotal, decimal Tax, decimal Total);
