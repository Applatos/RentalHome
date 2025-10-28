using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Contracts.Dtos.Admin.Pricing;

public record RatePlanDetailsDto(
    Guid Id,
    Guid HouseId,
    string Name,
    string Currency,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime? UpdatedUtc,
    IReadOnlyList<RateSeasonDetailsDto> Seasons);

public record RateSeasonDetailsDto(
    Guid Id,
    Guid RatePlanId,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal NightlyPrice,
    int? MinStayNights);

public record UpsertRatePlanDto(
    Guid? PlanId,
    string Name,
    string Currency,
    bool IsActive,
    IReadOnlyList<UpsertRateSeasonDto> Seasons);

public record UpsertRateSeasonDto(
    Guid? Id,
    string Name,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal NightlyPrice,
    int? MinStayNights);