using System.Collections.Generic;

namespace Sommerhus.Contracts.Dtos.Pricing;

public record PriceQuoteRequestDto(Guid HouseId, DateOnly Arrival, DateOnly Departure, int Guests, Guid? AreaId);

public record PriceQuoteLineItemDto(string Code, string Text, decimal Amount);

public record PriceQuoteResponseDto(string Currency, int Nights, IReadOnlyList<PriceQuoteLineItemDto> Items, decimal Subtotal, decimal Tax, decimal Total);