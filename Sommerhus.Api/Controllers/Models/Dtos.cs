namespace Sommerhus.Api.Controllers.Models;

// Images
public record HouseImageDto(Guid Id, string Url, string? Alt, string Kind);

// Features
public record FeatureDto(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);

public record FeatureValueDto(
    Guid FeatureId, string Name, string Key, string ValueType, string? Unit, string? IconUrl,
    bool? Bool, int? Int, decimal? Decimal, string? Text, string Display);

public record CreateHouseFeatureValueDto(Guid FeatureId, bool? ValueBool, int? ValueInt, decimal? ValueDecimal, string? ValueText);

// Houses
public record HouseListItemDto(Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? Cover);
public record HouseDetailsDto(
    Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? Description, string? Facilities,
    string? CoverUrl, HouseImageDto[] Gallery, HouseImageDto? Floorplan, FeatureValueDto[] Features);

// Zip/City
public record ZipCodeDto(string Zip, string City);
public record CityListItemDto(string Slug, string City, int Count);
