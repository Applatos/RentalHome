using System.Collections.Generic;

namespace Sommerhus.Api.Dtos.Public.Areas;

public record AreaListItemDto(
    Guid Id,
    string Slug,
    string Name,
    string? City,
    int HouseCount,
    string? Summary,
    string? HeroImageUrl);

public record AreaDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? City,
    string? Description,
    IReadOnlyList<AreaImageDto> Images,
    IReadOnlyList<AreaHouseDto> Houses);

public record AreaImageDto(Guid Id, string Url);

public record AreaHouseDto(Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? CoverUrl);
