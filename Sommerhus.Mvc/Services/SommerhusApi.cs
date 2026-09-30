using System.Globalization;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.Services;

public sealed class SommerhusApi
{
    private readonly HttpClient http;

    public SommerhusApi(HttpClient http) => this.http = http;

    public Task<ApiResponse<PageResult<PublicHouseListItemDto>?>> GetHousesAsync(
        HouseSearchFilter filter,
        CancellationToken ct = default)
    {
        var filters = new List<string>();
        // Invariant, like every number and date sent to the API: under da-DK a min price of
        // 700.5 would otherwise go out as "700,5" and be read back as 7005.
        void Add(FormattableString parameter) => filters.Add(FormattableString.Invariant(parameter));

        if (!string.IsNullOrWhiteSpace(filter.Query)) Add($"q={Uri.EscapeDataString(filter.Query.Trim())}");
        if (!string.IsNullOrWhiteSpace(filter.City)) Add($"city={Uri.EscapeDataString(filter.City.Trim())}");
        if (filter.AreaId.HasValue && filter.AreaId.Value != Guid.Empty) Add($"area={filter.AreaId.Value}");
        if (filter.MinPrice.HasValue) Add($"minPrice={filter.MinPrice.Value}");
        if (filter.MaxPrice.HasValue) Add($"maxPrice={filter.MaxPrice.Value}");
        if (filter.MinBedrooms.HasValue) Add($"minBedrooms={filter.MinBedrooms.Value}");
        if (filter.MinGuests.HasValue) Add($"minGuests={filter.MinGuests.Value}");
        if (filter.HasPool.HasValue) Add($"hasPool={filter.HasPool.Value.ToString().ToLowerInvariant()}");
        if (filter.PetFriendly.HasValue) Add($"petFriendly={filter.PetFriendly.Value.ToString().ToLowerInvariant()}");
        if (filter.Sort != HouseSearchSort.Relevance) Add($"sort={filter.Sort}");
        if (filter.Page > 1) Add($"page={filter.Page}");
        if (filter.PageSize != 20) Add($"pageSize={Math.Min(filter.PageSize, 100)}");

        if (filter.FeatureFilters is { Count: > 0 })
        {
            foreach (var entry in filter.FeatureFilters)
            {
                if (string.IsNullOrWhiteSpace(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                filters.Add($"f_{Uri.EscapeDataString(entry.Key)}={Uri.EscapeDataString(entry.Value)}");
            }
        }

        var url = filters.Count > 0 ? $"api/houses?{string.Join("&", filters)}" : "api/houses";
        return ApiHttp.GetAsync<PageResult<PublicHouseListItemDto>?>(http, url, ct);
    }

    public Task<ApiResponse<Dictionary<string, IReadOnlyList<SearchableFeatureDto>>?>> GetSearchableFeaturesAsync(CancellationToken ct = default)
        => ApiHttp.GetAsync<Dictionary<string, IReadOnlyList<SearchableFeatureDto>>?>(http, "api/features/searchable", ct);

    public Task<ApiResponse<PublicHouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct = default)
        => ApiHttp.GetAsync<PublicHouseDetailsDto?>(http, $"api/houses/{id}", ct);

    public Task<ApiResponse<PriceQuoteResponseDto?>> GetPriceQuoteAsync(PriceQuoteRequestDto request, CancellationToken ct = default)
        => ApiHttp.GetAsync<PriceQuoteResponseDto?>(
            http,
            string.Create(CultureInfo.InvariantCulture,
                $"api/houses/{request.HouseId}/quote?checkIn={request.Arrival:yyyy-MM-dd}&checkOut={request.Departure:yyyy-MM-dd}&guests={request.Guests}"),
            ct);

    public Task<ApiResponse<IReadOnlyList<AvailabilityBlockDto>?>> GetHouseAvailabilityAsync(
        Guid houseId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
        => ApiHttp.GetAsync<IReadOnlyList<AvailabilityBlockDto>?>(
            http,
            string.Create(CultureInfo.InvariantCulture, $"api/houses/{houseId}/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"),
            ct);

    public Task<ApiResponse<IReadOnlyList<AreaListItemDto>?>> GetAreasAsync(string? q = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(q)
            ? "api/areas"
            : $"api/areas?q={Uri.EscapeDataString(q.Trim())}";
        return ApiHttp.GetAsync<IReadOnlyList<AreaListItemDto>?>(http, url, ct);
    }

    public Task<ApiResponse<AreaDetailsDto?>> GetAreaAsync(Guid id, CancellationToken ct = default)
        => ApiHttp.GetAsync<AreaDetailsDto?>(http, $"api/areas/{id}", ct);
}
