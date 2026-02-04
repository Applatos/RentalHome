using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Unified City DTO for both list and details views.
/// Optional fields are populated for details/admin views.
/// </summary>
public sealed record CityDto(
    Guid Id,
    string Name,
    string Zip,
    // Optional fields for details/admin views
    string? Text = null,
    ImageDto[]? Images = null,
    int? HouseCount = null,
    int? ImageCount = null);

/// <summary>
/// City create/update DTO.
/// </summary>
public sealed record UpsertCityDto(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(10)] string Zip,
    [MaxLength(2000)] string? Text = null);
