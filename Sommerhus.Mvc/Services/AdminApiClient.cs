using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Shared;


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




    // ===== Features =====
    public Task<ApiResult<object?>> UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto> values, CancellationToken ct = default)
    {
        var payload = values?.ToList() ?? new List<PostFeatureValueDto>();
        return PostAsync<IEnumerable<PostFeatureValueDto>, object>($"api/admin/houses/{houseId}/features", payload, ct);
    }

    public Task<ApiResult<IReadOnlyList<FeatureDetailsDto>?>> GetFeaturesAsync(CancellationToken ct = default)
     => GetAsync<IReadOnlyList<FeatureDetailsDto>>("api/admin/features", ct);

    public Task<ApiResult<Guid>> CreateFeatureAsync(UpsertFeatureDto dto, CancellationToken ct = default)
    => PostAsync<UpsertFeatureDto, Guid>("api/admin/features", dto, ct);

    public Task<ApiResult<object?>> UpdateFeatureAsync(Guid id, UpsertFeatureDto dto, CancellationToken ct = default)
    => PutAsync<UpsertFeatureDto, object>($"api/admin/features/{id}", dto, ct);

    public Task<ApiResult<object?>> DeleteFeatureAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync($"api/admin/features/{id}", ct);

    public Task<ApiResult<object?>> UploadFeatureIconAsync(Guid featureId, Stream stream, string fileName, string? contentType, CancellationToken ct = default)
    {
        var tuple = (stream, fileName, contentType);
        return PostMultipartAsync<object>($"api/admin/features/{featureId}/icon", "file", new[]{ tuple }, ct);
    }






//// ===== Areas =====
    public Task<ApiResult<IReadOnlyList<AreaListItemDto>?>> GetAreasAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<AreaListItemDto>>("api/admin/areas", ct);

    public Task<ApiResult<AreaDetailsDto?>> GetAreaAsync(Guid id, CancellationToken ct = default)
        => GetAsync<AreaDetailsDto>($"api/admin/areas/{id}", ct);

    public Task<ApiResult<AreaDetailsDto?>> CreateAreaAsync(UpsertAreaDto dto, CancellationToken ct = default)
        => PostAsync<UpsertAreaDto, AreaDetailsDto>("api/admin/areas", dto, ct);

    public Task<ApiResult<object?>> UpdateAreaAsync(Guid id, UpsertAreaDto dto, CancellationToken ct = default)
        => PutAsync<UpsertAreaDto, object>($"api/admin/areas/{id}", dto, ct);

    public Task<ApiResult<object?>> DeleteAreaAsync(Guid id, CancellationToken ct = default)
        => DeleteAsync($"api/admin/areas/{id}", ct);

    public Task<ApiResult<IReadOnlyList<LookupItem>?>> GetAreasLookupAsync(CancellationToken ct = default)
    => GetAsync<IReadOnlyList<LookupItem>>("api/admin/areas/lookup", ct);


    public Task<ApiResult<IReadOnlyList<ImageDto>?>> GetAreaImagesAsync(Guid areaId, CancellationToken ct = default)
    => GetAsync<IReadOnlyList<ImageDto>>($"api/admin/areas/{areaId}/images", ct);

    public Task<ApiResult<ImageDto?>> UploadAreaImageAsync(Guid areaId, IFormFile file, CancellationToken ct = default)
    {
        var tuple = (file.OpenReadStream(), file.FileName, file.ContentType);
        return PostMultipartAsync<ImageDto>($"api/admin/areas/{areaId}/images", "file", new[] { tuple }, ct);
    }

    public Task<ApiResult<object?>> DeleteAreaImageAsync(Guid areaId, Guid imageId, CancellationToken ct = default)
        => DeleteAsync($"api/admin/areas/{areaId}/images/{imageId}", ct);


    // ===== Pricing =====

    public Task<ApiResult<PricePlanDetailsDto?>> PutHousePricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
        => PutAsync<PricePlanDetailsDto, PricePlanDetailsDto>($"api/admin/houses/{houseId}/pricing", dto, ct);

    public Task<ApiResult<IReadOnlyList<SeasonCodeDto>?>> GetSeasonCodesAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SeasonCodeDto>>("api/season-codes", ct);

    public Task<ApiResult<object?>> DeleteHouseRatePlanAsync(Guid houseId, Guid ratePlanId, CancellationToken ct)
        => DeleteAsync($"api/admin/houses/{houseId}/pricing/rate-plans/{ratePlanId}", ct);




    // ===== House groups & season codes =====
    public Task<ApiResult<IReadOnlyList<LookupItem>?>> GetHouseGroupsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<LookupItem>>("api/admin/house-groups", ct);

    public Task<ApiResult<LookupItem?>> CreateHouseGroupAsync(HouseGroupDto dto, CancellationToken ct = default)
        => PostAsync<HouseGroupDto, LookupItem>("api/admin/house-groups", dto, ct);

    public Task<ApiResult<SeasonCodeDto?>> CreateSeasonCodeAsync(SeasonCodeDto dto, CancellationToken ct = default)
        => PostAsync<SeasonCodeDto, SeasonCodeDto>("api/season-codes", dto, ct);

}






