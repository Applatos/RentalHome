namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// House list item with optional admin/public-specific fields.
/// </summary>
public sealed record HouseListItemDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features,
    
    // Admin-specific fields
    string? Name = null,                    // Admin uses Name, Public uses Title
    string? CityLabel = null,               // Admin uses CityLabel, Public uses City/Zip
    IReadOnlyList<string>? AreaLabels = null, // Admin only
    DateTime? CreatedUtc = null,              // Admin only
    string? CoverUrl = null,                 // Public list item field
    string? Summary = null,                 // Public list item field
    IReadOnlyList<ImageDto>? Gallery = null  // Public list item field
);
