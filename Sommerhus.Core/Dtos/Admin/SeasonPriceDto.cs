namespace Sommerhus.Core.Dtos.Admin;

public record SeasonPriceDto(
    Guid Id, 
    Guid RatePlanId, 
    string Code, 
    decimal NightlyPrice);
