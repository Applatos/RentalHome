using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Contracts.Dtos.Public.Houses;

public record HouseListItemDto(
    Guid Id,
    string Title,
    string City,
    string Zip,
    string? CoverUrl,
    string Summary,
    IReadOnlyList<ImageDto> Gallery);