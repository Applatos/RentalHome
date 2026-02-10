using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// House details for admin context — all admin fields are required.
/// </summary>
public sealed record AdminHouseDetailsDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features,
    Guid CityId,
    string? CityLabel,
    IReadOnlyList<Guid> AreaIds,
    IReadOnlyList<LookupItem> Areas,
    DateTime CreatedAtUtc,
    IReadOnlyList<SeasonSpanDto>? Calendar,
    PricePlanDetailsDto? Pricing,
    Guid? GroupId,
    EntityStatus Status,
    string? SearchKeywords = null,
    DateTime? PublishedAtUtc = null,
    DateTime? ArchivedAtUtc = null,
    Guid? CalendarOverrideId = null,
    string? CalendarSource = null);

/// <summary>
/// House details for public context — only public-relevant fields.
/// </summary>
public sealed record PublicHouseDetailsDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features);
