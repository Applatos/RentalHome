using System.Collections.Generic;

namespace Sommerhus.Core.Dtos.Shared;

/// <summary>
/// Area details with optional admin-specific fields.
/// </summary>
public sealed record AreaDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    
    // Admin-specific fields
    IReadOnlyList<Guid>? CityIds = null,
    IReadOnlyList<LookupItem>? Cities = null,
    IReadOnlyList<AreaHouseDto>? Houses = null);

public sealed record AreaHouseDto(Guid Id, string Title);
