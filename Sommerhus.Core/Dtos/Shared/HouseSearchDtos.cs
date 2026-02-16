namespace Sommerhus.Core.Dtos.Shared;

public enum HouseSearchSort
{
    Relevance = 0,
    PriceAsc = 1,
    PriceDesc = 2,
    Newest = 3
}

public sealed record HouseSearchFilter
{
    public string? Query { get; init; }
    public Guid? AreaId { get; init; }
    public string? City { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinBedrooms { get; init; }
    public int? MinGuests { get; init; }
    public bool? HasPool { get; init; }
    public bool? PetFriendly { get; init; }
    public Dictionary<string, string>? FeatureFilters { get; init; }
    public DateOnly? CheckIn { get; init; }
    public DateOnly? CheckOut { get; init; }
    public HouseSearchSort Sort { get; init; } = HouseSearchSort.Relevance;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed record HouseSearchResultDto(
    Guid Id,
    string Title,
    string? City,
    string? Zip,
    string? Address,
    string? Description,
    IReadOnlyList<ImageDto> Images,
    IReadOnlyList<FeatureValueDto> Features,
    decimal? MinNightlyPrice,
    string? Currency,
    string? CoverUrl,
    string? Summary,
    IReadOnlyList<ImageDto> Gallery);
