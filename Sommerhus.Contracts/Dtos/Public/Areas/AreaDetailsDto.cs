using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Public.Areas;

public record AreaDetailsDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    IReadOnlyList<ImageDto> Images);
