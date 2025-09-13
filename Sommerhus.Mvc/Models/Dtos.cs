namespace Sommerhus.Mvc.Models;

// BILLEDER
public enum ImageKind { Gallery = 0, Cover = 1, Floorplan = 2 }
public record ImageDto(int Id, string Url, int SortOrder, ImageKind Kind, string? Alt);

// FEATURES
public enum FeatureValueType { None = 0, Bool = 1, Int = 2, Decimal = 3, Text = 4 }
public record FeatureDto(int Id, string Name, string Icon, FeatureValueType ValueType, string? Unit);

public record HouseFeatureValueDto(
    int FeatureId, string Name, string Icon, FeatureValueType ValueType, string? Unit,
    bool? ValueBool, int? ValueInt, decimal? ValueDecimal, string? ValueText
);

// HUSE
public record VacationHouseListDto(
    Guid Id, string Title, string Subtitle, string City, string Zip,
    string? CoverImageUrl, List<string> FeatureLabels
);

public record VacationHouseDetailDto(
    Guid Id, string Title, string Subtitle, string Description,
    string Address, string City, string Zip,
    List<ImageDto> Images, List<HouseFeatureValueDto> Features
);

// Create/Update
public record CreateHouseFeatureValueDto(
    int FeatureId, bool? ValueBool, int? ValueInt, decimal? ValueDecimal, string? ValueText
);

public record CreateVacationHouseDto(
    string Title, string Subtitle, string Description,
    string Address, string City, string Zip,
    List<string> ImageUrls, List<CreateHouseFeatureValueDto> Features
);

public record UpdateVacationHouseDto(
    string Title, string Subtitle, string Description,
    string Address, string City, string Zip,
    List<string> ImageUrls, List<CreateHouseFeatureValueDto> Features
);

public record CityDto(string Name, string Slug, int HouseCount);
public record UpdateImageMetaDto(int Id, ImageKind Kind, int SortOrder, string? Alt);
public record UpdateImagesMetaDto(List<UpdateImageMetaDto> Items);
