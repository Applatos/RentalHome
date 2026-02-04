using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// House details with optional admin-specific fields.
/// </summary>
public sealed record HouseDetailsDto(
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
    Guid? CityId = null,                    // Admin uses CityId, Public uses City/Zip
    string? CityLabel = null,               // Admin uses CityLabel, Public uses City/Zip
    IReadOnlyList<Guid>? AreaIds = null,     // Admin only
    IReadOnlyList<LookupItem>? Areas = null, // Admin only
    DateTime? CreatedUtc = null,              // Admin only
    IReadOnlyList<SeasonSpanDto>? Calendar = null, // Admin only
    PricePlanDetailsDto? Pricing = null,     // Admin only
    Guid? GroupId = null,                   // Admin only
    string? CoverUrl = null,                 // Public list item field
    string? Summary = null,                 // Public list item field
    IReadOnlyList<ImageDto>? Gallery = null  // Public list item field
);
