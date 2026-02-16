using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Mvc.Services;

public sealed class SommerhusApi
{
    private readonly HttpClient http;

    public SommerhusApi(HttpClient http) => this.http = http;

    public Task<ApiResponse<PageResult<PublicHouseListItemDto>?>> GetHousesAsync(string? q = null, Guid? areaId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(q)) filters.Add($"q={Uri.EscapeDataString(q.Trim())}");
        if (areaId.HasValue && areaId.Value != Guid.Empty) filters.Add($"area={areaId.Value}");
        if (page > 1) filters.Add($"page={page}");
        if (pageSize != 20) filters.Add($"pageSize={Math.Min(pageSize, 100)}");

        var url = filters.Count > 0 ? $"api/houses?{string.Join("&", filters)}" : "api/houses";
        return ApiHttp.GetAsync<PageResult<PublicHouseListItemDto>?>(http, url, ct);
    }

    public Task<ApiResponse<PublicHouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct = default)
        => ApiHttp.GetAsync<PublicHouseDetailsDto?>(http, $"api/houses/{id}", ct);

    public Task<ApiResponse<PriceQuoteResponseDto?>> GetPriceQuoteAsync(PriceQuoteRequestDto request, CancellationToken ct = default)
        => ApiHttp.GetAsync<PriceQuoteResponseDto?>(
            http,
            $"api/houses/{request.HouseId}/quote?checkIn={request.Arrival:yyyy-MM-dd}&checkOut={request.Departure:yyyy-MM-dd}&guests={request.Guests}",
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
