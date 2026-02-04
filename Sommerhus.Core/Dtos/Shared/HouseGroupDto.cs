using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Unified HouseGroup DTO for both list and details views.
/// Optional fields are populated for details views.
/// </summary>
public sealed record HouseGroupDto(
    Guid Id,
    string Name,
    // Optional fields for details views
    int? HouseCount = null,
    IReadOnlyList<SeasonSpanDto>? Calendar = null);

/// <summary>
/// HouseGroup create/update DTO.
/// </summary>
public sealed record UpsertHouseGroupDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = "";
}
