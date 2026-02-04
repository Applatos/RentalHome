using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Dtos.Public.Houses;

public record HouseListItemDto(
    Guid Id,
    string Title,
    string City,
    string Zip,
    string? CoverUrl,
    string Summary,
    IReadOnlyList<ImageDto> Gallery);
