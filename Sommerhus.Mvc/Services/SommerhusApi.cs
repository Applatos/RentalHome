using Sommerhus.Contracts.Dtos.Public.Areas;
using Sommerhus.Contracts.Dtos.Public.Cities;
using Sommerhus.Contracts.Dtos.Public.Houses;


namespace Sommerhus.Mvc.Services;

public sealed class SommerhusApi : ApiClientBase
{
    public SommerhusApi(HttpClient http) : base(http) { }

    // ===== Public API =====

    // ===== Houses =====
    public Task<ApiResult<IReadOnlyList<HouseListItemDto>>> GetHousesAsync(string? q = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q.Trim())}");
        if (skip > 0) qs.Add($"skip={skip}");
        if (take > 0) qs.Add($"take={Math.Min(take, 100)}");

        var url = "api/houses" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return GetAsync<IReadOnlyList<HouseListItemDto>>(url, ct)!;
    }

    public Task<ApiResult<HouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct = default)
        => GetAsync<HouseDetailsDto>($"api/houses/{id}", ct);

    public Task<ApiResult<IReadOnlyList<LookupItem>?>> GetCitiesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LookupItem>>("api/admin/cities/search", ct);



    // ===== Areas =====
    public Task<ApiResult<IReadOnlyList<AreaListItemDto>?>> GetAreasAsync(string? q = null, CancellationToken ct = default)
    {
        var url = string.IsNullOrWhiteSpace(q)
            ? "api/areas"
            : $"api/areas?q={Uri.EscapeDataString(q.Trim())}";
        return GetAsync<IReadOnlyList<AreaListItemDto>>(url, ct);
    }

    public Task<ApiResult<AreaDetailDto?>> GetAreaAsync(Guid id, CancellationToken ct = default)
        => GetAsync<AreaDetailDto>($"api/areas/{id}", ct);

    //
}
