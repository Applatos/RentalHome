namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// House list item for admin context.
/// </summary>
public sealed record AdminHouseListItemDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    string? CityLabel,
    IReadOnlyList<string> AreaLabels,
    DateTime CreatedAtUtc);

/// <summary>
/// House list item for public context.
/// </summary>
public sealed record PublicHouseListItemDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features,
    string? CoverUrl,
    string? Summary,
    IReadOnlyList<ImageDto> Gallery);
