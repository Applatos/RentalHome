using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Season span DTO for calendar display.
/// </summary>
public sealed record SeasonSpanDto(
    Guid Id, 
    DateOnly StartDate, 
    DateOnly EndDate, 
    string Code,
    string? SeasonName = null,
    string? Color = null);

/// <summary>
/// Season span create/update DTO.
/// </summary>
public sealed class UpsertSeasonSpanDto
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Required, MaxLength(10)]
    public string Code { get; set; } = "";
}

/// <summary>
/// Season code definition DTO.
/// </summary>
public sealed record SeasonCodeDto(
    string Code,
    string? Label,
    string? Color,
    int SortOrder);

/// <summary>
/// Season price DTO.
/// </summary>
public sealed record SeasonPriceDto(
    Guid Id, 
    Guid RatePlanId, 
    string Code, 
    decimal NightlyPrice);

/// <summary>
/// Price plan details DTO.
/// </summary>
public sealed record PricePlanDetailsDto(
    Guid PlanId, 
    Guid HouseId, 
    string Name, 
    string Currency, 
    bool IsActive,   
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<SeasonPriceDto> SeasonPrices);
