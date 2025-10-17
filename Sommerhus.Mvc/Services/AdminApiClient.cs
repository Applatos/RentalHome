using Microsoft.AspNetCore.Mvc.Rendering;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;
using AdmAreas = Sommerhus.Contracts.Dtos.Admin.Areas;
using AdmFeats = Sommerhus.Contracts.Dtos.Admin.Features;
using AdmHouses = Sommerhus.Contracts.Dtos.Admin.Houses;
using PubCities = Sommerhus.Contracts.Dtos.Public.Cities;

namespace Sommerhus.Mvc.Services;


public sealed class AdminApiClient : ApiClientBase
{
    public AdminApiClient(HttpClient http) : base(http) { }

    // ===== Houses =====
    public Task<ApiResult<PageResult<HouseListItemDto>>> GetHousesAsync(string? q, int page, int pageSize, CancellationToken ct)
        => GetAsync<PageResult<HouseListItemDto>>($"api/admin/houses?query={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}", ct)!;

    public Task<ApiResult<HouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct)
        => GetAsync<HouseDetailsDto>($"api/admin/houses/{id}", ct);

    public Task<ApiResult<Guid>> PostHouseAsync(UpsertHouseDto dto, CancellationToken ct)
        => PostAsync<UpsertHouseDto, Guid>("api/admin/houses", dto, ct);

    public Task<ApiResult<object?>> PutHouseAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
        => PutAsync<UpsertHouseDto, object>($"api/admin/houses/{id}", dto, ct);

    public Task<ApiResult<object?>> DeleteHouseAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync($"api/admin/houses/{id}", ct);

    public Task<ApiResult<IReadOnlyList<LookupItem>?>> GetCitiesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LookupItem>>("api/admin/cities/lookup", ct);

    public Task<ApiResult<IReadOnlyList<ImageDto>?>> UploadHouseImagesAsync(Guid houseId, IEnumerable<IFormFile> files, CancellationToken ct)
    {
        var tuples = files.Select(f => (f.OpenReadStream(), f.FileName, f.ContentType));
        return PostMultipartAsync<IReadOnlyList<ImageDto>>($"api/admin/houses/{houseId}/images", "files", tuples, ct);
    }
    public Task<ApiResult<object?>> DeleteHouseImageAsync(Guid houseId, Guid imageId, CancellationToken ct)
        => DeleteAsync($"api/admin/houses/{houseId}/images/{imageId}", ct);

        // NEW: set-kind endpoint (no body)
    public Task<ApiResult<object?>> SetHouseImageKindAsync(Guid houseId, Guid imageId, string kind, CancellationToken ct = default)
        => PostAsync<object, object>($"api/admin/houses/{houseId}/images/{imageId}/set-kind?kind={Uri.EscapeDataString(kind)}", new { }, ct);

    // NEW: upsert features for a house
    //public Task<ApiResult<object?>> UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<AdmFeats.PostFeatureValueDto> values, CancellationToken ct = default)
    //    => PostAsync<IEnumerable<AdmFeats.PostFeatureValueDto>, object>($"api/admin/houses/{houseId}/features", values, ct);


}



    // ===== Features =====

//    public Task<bool> UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto> values, CancellationToken ct)
//        => PostAsync($"api/admin/houses/{houseId}/features", values, ct);


//    public async Task<(bool ok, IReadOnlyList<ImageDto>? images)> UploadHouseImagesAsync(Guid houseId, IEnumerable<IFormFile> files, CancellationToken ct)
//    {
//        using var form = new MultipartFormDataContent();
//        foreach (var f in files)
//        {
//            var sc = new StreamContent(f.OpenReadStream());
//            if (!string.IsNullOrWhiteSpace(f.ContentType))
//                sc.Headers.ContentType = new MediaTypeHeaderValue(f.ContentType);
//            form.Add(sc, "files", f.FileName);
//        }
//        var res = await http.PostAsync($"api/admin/houses/{houseId}/images", form, ct);
//        if (!res.IsSuccessStatusCode) return (false, null);
//        var data = await res.Content.ReadFromJsonAsync<List<ImageDto>>(cancellationToken: ct);
//        return (true, data ?? []);
//    }

//    public Task<bool> SetHouseImageKindAsync(Guid houseId, Guid imageId, string kind, CancellationToken ct)
//        => PostAsync<object>($"api/admin/houses/{houseId}/images/{imageId}/set-kind?kind={kind}", new { }, ct);






//// ===== Areas =====
//    public Task<IReadOnlyList<AdmAreas.AreaListItemDto>> GetAreasAsync(CancellationToken ct)
//            => GetListAsync<AdmAreas.AreaListItemDto>("api/admin/areas", ct);

//    public Task<AdmAreas.AreaDetailsDto?> GetAreaAsync(Guid id, CancellationToken ct)
//        => GetAsync<AdmAreas.AreaDetailsDto>($"api/admin/areas/{id}", ct);

//    public Task<ApiResult<AdmAreas.AreaDetailsDto?>> CreateAreaAsync(AdmAreas.UpsertAreaDto dto, CancellationToken ct)
//        => PostForResultAsync<AdmAreas.UpsertAreaDto, AdmAreas.AreaDetailsDto>("api/admin/areas", dto, ct);

//    public Task<ApiResult<object?>> UpdateAreaAsync(Guid id, AdmAreas.UpsertHouseDto dto, CancellationToken ct)
//        => PutForResultAsync($"api/admin/areas/{id}", dto, ct);

//    public Task<ApiResult<object?>> DeleteAreaAsync(Guid id, CancellationToken ct)
//        => DeleteForResultAsync($"api/admin/areas/{id}", ct);









//    // ===== Cities (admin lister) =====
//    //public Task<IReadOnlyList<PubCities.CityListItemDto>> GetCitiesAsync(CancellationToken ct)
//    //    => GetListAsync<PubCities.CityListItemDto>("api/admin/cities", ct);









//    // ===== Features =====
//    public Task<IReadOnlyList<AdmFeats.FeatureDetailsDto>> GetFeaturesAsync(CancellationToken ct)
//        => GetListAsync<AdmFeats.FeatureDetailsDto>("api/admin/features", ct);

//    public async Task<(bool ok, Guid? id)> CreateFeatureAsync(AdmFeats.UpsertFeatureDto dto, CancellationToken ct)
//    {
//        var (ok, data) = await PostAsync<AdmFeats.UpsertFeatureDto, Guid>("api/admin/features", dto, ct);
//        return ok ? (true, (Guid?)data) : (false, (Guid?)null);
//    }
//    public Task<bool> UpdateFeatureAsync(Guid id, AdmFeats.UpsertFeatureDto dto, CancellationToken ct)
//        => PutAsync($"api/admin/features/{id}", dto, ct);

//    public Task<bool> DeleteFeatureAsync(Guid id, CancellationToken ct)
//        => DeleteOkOrNotFoundAsync($"api/admin/features/{id}", ct);
//}
