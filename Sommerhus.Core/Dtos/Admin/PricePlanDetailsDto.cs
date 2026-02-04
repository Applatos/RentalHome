namespace Sommerhus.Core.Dtos.Admin;

public record PricePlanDetailsDto(
    Guid planId, 
    Guid HouseId, 
    string Name, 
    string Currency, 
    bool IsActive,   
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    IReadOnlyList<SeasonPriceDto> SeasonPrices);
