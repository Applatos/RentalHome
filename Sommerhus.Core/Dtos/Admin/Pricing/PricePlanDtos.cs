using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Core.Dtos.Admin.Pricing;


// General price plan dtos (Use cases: get and post plan data)
public record PricePlanDetailsDto(
    Guid planId, 
    Guid HouseId, 
    string Name, 
    string Currency, 
    bool IsActive,   
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    IReadOnlyList<SeasonPriceDto> SeasonPrices);


// Season Price dtos (Use cases: get and post season prices for a rate plan)
public record SeasonPriceDto(
    Guid Id, 
    Guid RatePlanId, 
    string Code, 
    decimal NightlyPrice);

// Season House Span dtos (Use cases: get and post season house spans for a rate plan)
public record SeasonSpanDto(
    Guid Id, 
    DateOnly StartDate, 
    DateOnly EndDate, 
    string Code,
    string? SeasonName = null,
    string? Color = null);

public record SeasonCodeDto(
    string Code,
    string? Label,
    string? Color,
    int SortOrder);
