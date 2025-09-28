using AdmHouses = Sommerhus.Contracts.Dtos.Admin.Houses;
using AdmAreas = Sommerhus.Contracts.Dtos.Admin.Areas;
using AdmFeats = Sommerhus.Contracts.Dtos.Admin.Features;
using PubCities = Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiClient : ApiClientBase
{
    public AdminApiClient(HttpClient http) : base(http) { }

    // ===== Houses =====
    public Task<AdmHouses.HousesPageDto> SearchHousesAsync(string? q, int page, int pageSize, CancellationToken ct)
        => GetAsync<AdmHouses.HousesPageDto>($"api/admin/houses?query={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}", ct)!;

    public Task<AdmHouses.HouseAdminDetailsDto?> GetHouseAsync(Guid id, CancellationToken ct)
        => GetAsync<AdmHouses.HouseAdminDetailsDto>($"api/admin/houses/{id}", ct);

    public async Task<(bool ok, Guid? id)> CreateHouseAsync(AdmHouses.CreateHouseDto dto, CancellationToken ct)
    {
        var (ok, data) = await PostAsync<AdmHouses.CreateHouseDto, Guid>("api/admin/houses", dto, ct);
        return ok ? (true, (Guid?)data) : (false, (Guid?)null);
    }
    public Task<bool> UpdateHouseAsync(Guid id, AdmHouses.UpdateHouseDto dto, CancellationToken ct)
        => PutAsync($"api/admin/houses/{id}", dto, ct);

    public Task<bool> DeleteHouseAsync(Guid id, CancellationToken ct)
        => DeleteOkOrNotFoundAsync($"api/admin/houses/{id}", ct);

    public Task<bool> DeleteHouseImageAsync(Guid houseId, Guid imageId, CancellationToken ct)
        => DeleteOkOrNotFoundAsync($"api/admin/houses/{houseId}/images/{imageId}", ct);

    public Task<bool> UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<AdmFeats.PostFeatureValueDto> values, CancellationToken ct)
        => PostAsync($"api/admin/houses/{houseId}/features", values, ct);

    // ===== Areas =====
    public Task<IReadOnlyList<AdmAreas.AreaListItemDto>> GetAreasAsync(CancellationToken ct)
        => GetListAsync<AdmAreas.AreaListItemDto>("api/admin/areas", ct);

    // Navn i Contracts: AreaDetailDto (ikke AreaDetailsDto)
    public Task<AdmAreas.AreaDetailsDto?> GetAreaAsync(Guid id, CancellationToken ct)
        => GetAsync<AdmAreas.AreaDetailsDto>($"api/admin/areas/{id}", ct);

    // ===== Cities (admin lister) =====
    public Task<IReadOnlyList<PubCities.CityListItemDto>> GetCitiesAsync(CancellationToken ct)
        => GetListAsync<PubCities.CityListItemDto>("api/admin/cities", ct);

    // ===== Features =====
    public Task<IReadOnlyList<AdmFeats.FeatureDto>> GetFeaturesAsync(CancellationToken ct)
        => GetListAsync<AdmFeats.FeatureDto>("api/admin/features", ct);

    public async Task<(bool ok, Guid? id)> CreateFeatureAsync(AdmFeats.UpsertFeatureDto dto, CancellationToken ct)
    {
        var (ok, data) = await PostAsync<AdmFeats.UpsertFeatureDto, Guid>("api/admin/features", dto, ct);
        return ok ? (true, (Guid?)data) : (false, (Guid?)null);
    }
    public Task<bool> UpdateFeatureAsync(Guid id, AdmFeats.UpsertFeatureDto dto, CancellationToken ct)
        => PutAsync($"api/admin/features/{id}", dto, ct);

    public Task<bool> DeleteFeatureAsync(Guid id, CancellationToken ct)
        => DeleteOkOrNotFoundAsync($"api/admin/features/{id}", ct);
}
