using PubHouses = Sommerhus.Contracts.Dtos.Public.Houses;
using PubAreas = Sommerhus.Contracts.Dtos.Public.Areas;
using PubCities = Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Mvc.Services;

public sealed class SommerhusApi : ApiClientBase
{
    public SommerhusApi(HttpClient http) : base(http) { }

    public async Task<IReadOnlyList<PubHouses.HouseListItemDto>> GetHousesAsync(
        string? citySlug = null, string? q = null, int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(citySlug)) qs.Add($"city={Uri.EscapeDataString(citySlug.Trim().ToLowerInvariant())}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q.Trim())}");
        if (skip > 0) qs.Add($"skip={skip}");
        if (take > 0) qs.Add($"take={Math.Min(take, 100)}");

        var url = "api/houses" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await GetListAsync<PubHouses.HouseListItemDto>(url, ct);
    }

    public Task<PubHouses.HouseDetailsDto?> GetHouseAsync(Guid id, CancellationToken ct = default)
        => GetAsync<PubHouses.HouseDetailsDto>($"api/houses/{id}", ct);

    public Task<IReadOnlyList<PubAreas.AreaListItemDto>> GetAreasAsync(string? q = null, CancellationToken ct = default)
        => GetListAsync<PubAreas.AreaListItemDto>(string.IsNullOrWhiteSpace(q) ? "api/areas" : $"api/areas?q={Uri.EscapeDataString(q)}", ct);

    public Task<PubAreas.AreaDetailsDto?> GetAreaBySlugAsync(string slug, CancellationToken ct = default)
        => GetAsync<PubAreas.AreaDetailsDto>($"api/areas/{Uri.EscapeDataString(slug)}", ct);

    public Task<IReadOnlyList<PubCities.CityListItemDto>> GetCitiesAsync(CancellationToken ct = default)
        => GetListAsync<PubCities.CityListItemDto>("api/cities", ct);

    public Task<IReadOnlyList<string>> FindZipAsync(string q, CancellationToken ct = default)
        => GetListAsync<string>($"api/zipcodes?q={Uri.EscapeDataString(q)}", ct);
}
