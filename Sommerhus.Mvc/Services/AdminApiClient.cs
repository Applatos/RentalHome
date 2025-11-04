using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiClient
{
    private readonly HttpClient http;

    public AdminApiClient(HttpClient http) => this.http = http;

    // Houses
    public Task<ApiResponse<PageResult<HouseListItemDto>>> GetHousesAsync(string? q, int page, int pageSize, CancellationToken ct)
        => ApiHttp.GetAsync<PageResult<HouseListItemDto>>(http, $"api/admin/houses?query={Uri.EscapeDataString(q ?? string.Empty)}&page={page}&pageSize={pageSize}", ct);

    public Task<ApiResponse<HouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct)
        => ApiHttp.GetAsync<HouseDetailsDto?>(http, $"api/admin/houses/{id}", ct);

    public Task<ApiResponse<Guid>> PostHouseAsync(UpsertHouseDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertHouseDto, Guid>(http, "api/admin/houses", dto, ct);

    public Task<ApiResponse<object?>> PutHouseAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertHouseDto, object?>(http, $"api/admin/houses/{id}", dto, ct);

    public Task<ApiResponse<object?>> DeleteHouseAsync(Guid id, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{id}", ct);

    public Task<ApiResponse<IReadOnlyList<LookupItem>?>> GetCitiesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<LookupItem>?>(http, "api/admin/cities/lookup", ct);

    public async Task<ApiResponse<IReadOnlyList<ImageDto>?>> UploadHouseImagesAsync(Guid houseId, IEnumerable<IFormFile> files, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            AddFile(form, "files", file.OpenReadStream(), file.FileName, file.ContentType);
        }

        return await ApiHttp.SendAsync<IReadOnlyList<ImageDto>?>(http, (client, token) => client.PostAsync($"api/admin/houses/{houseId}/images", form, token), ct);
    }

    public Task<ApiResponse<object?>> DeleteHouseImageAsync(Guid houseId, Guid imageId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{houseId}/images/{imageId}", ct);

    public Task<ApiResponse<object?>> SetHouseImageKindAsync(Guid houseId, Guid imageId, string kind, CancellationToken ct)
        => ApiHttp.SendAsync<object?>(http, (client, token) => client.PostAsync($"api/admin/houses/{houseId}/images/{imageId}/set-kind?kind={Uri.EscapeDataString(kind)}", content: null, token), ct);

    public Task<ApiResponse<object?>> UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto> values, CancellationToken ct)
        => ApiHttp.PostAsync<IEnumerable<PostFeatureValueDto>, object?>(http, $"api/admin/houses/{houseId}/features", values?.ToList() ?? new List<PostFeatureValueDto>(), ct);

    // Features
    public Task<ApiResponse<IReadOnlyList<FeatureDetailsDto>?>> GetFeaturesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<FeatureDetailsDto>?>(http, "api/admin/features", ct);

    public Task<ApiResponse<Guid>> CreateFeatureAsync(UpsertFeatureDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertFeatureDto, Guid>(http, "api/admin/features", dto, ct);

    public Task<ApiResponse<object?>> UpdateFeatureAsync(Guid id, UpsertFeatureDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertFeatureDto, object?>(http, $"api/admin/features/{id}", dto, ct);

    public Task<ApiResponse<object?>> DeleteFeatureAsync(Guid id, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/features/{id}", ct);

    public async Task<ApiResponse<object?>> UploadFeatureIconAsync(Guid featureId, Stream stream, string fileName, string? contentType, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        AddFile(form, "file", stream, fileName, contentType);
        return await ApiHttp.SendAsync<object?>(http, (client, token) => client.PostAsync($"api/admin/features/{featureId}/icon", form, token), ct);
    }

    // Areas
    public Task<ApiResponse<IReadOnlyList<AreaListItemDto>?>> GetAreasAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<AreaListItemDto>?>(http, "api/admin/areas", ct);

    public Task<ApiResponse<AreaDetailsDto?>> GetAreaAsync(Guid id, CancellationToken ct)
        => ApiHttp.GetAsync<AreaDetailsDto?>(http, $"api/admin/areas/{id}", ct);

    public Task<ApiResponse<AreaDetailsDto?>> CreateAreaAsync(UpsertAreaDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertAreaDto, AreaDetailsDto?>(http, "api/admin/areas", dto, ct);

    public Task<ApiResponse<object?>> UpdateAreaAsync(Guid id, UpsertAreaDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertAreaDto, object?>(http, $"api/admin/areas/{id}", dto, ct);

    public Task<ApiResponse<object?>> DeleteAreaAsync(Guid id, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/areas/{id}", ct);

    public Task<ApiResponse<IReadOnlyList<LookupItem>?>> GetAreasLookupAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<LookupItem>?>(http, "api/admin/areas/lookup", ct);

    public Task<ApiResponse<IReadOnlyList<ImageDto>?>> GetAreaImagesAsync(Guid areaId, CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<ImageDto>?>(http, $"api/admin/areas/{areaId}/images", ct);

    public async Task<ApiResponse<ImageDto?>> UploadAreaImageAsync(Guid areaId, IFormFile file, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        AddFile(form, "file", file.OpenReadStream(), file.FileName, file.ContentType);
        return await ApiHttp.SendAsync<ImageDto?>(http, (client, token) => client.PostAsync($"api/admin/areas/{areaId}/images", form, token), ct);
    }

    public Task<ApiResponse<object?>> DeleteAreaImageAsync(Guid areaId, Guid imageId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/areas/{areaId}/images/{imageId}", ct);

    // Pricing
    public Task<ApiResponse<PricePlanDetailsDto?>> PutHousePricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<PricePlanDetailsDto, PricePlanDetailsDto?>(http, $"api/admin/houses/{houseId}/pricing", dto, ct);

    public Task<ApiResponse<IReadOnlyList<SeasonCodeDto>?>> GetSeasonCodesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<SeasonCodeDto>?>(http, "api/season-codes", ct);

    public Task<ApiResponse<object?>> DeleteHouseRatePlanAsync(Guid houseId, Guid ratePlanId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{houseId}/pricing/rate-plans/{ratePlanId}", ct);

    // House groups & season codes
    public Task<ApiResponse<IReadOnlyList<LookupItem>?>> GetHouseGroupsAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<LookupItem>?>(http, "api/admin/house-groups", ct);

    public Task<ApiResponse<LookupItem?>> CreateHouseGroupAsync(HouseGroupDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<HouseGroupDto, LookupItem?>(http, "api/admin/house-groups", dto, ct);

    public Task<ApiResponse<SeasonCodeDto?>> CreateSeasonCodeAsync(SeasonCodeDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<SeasonCodeDto, SeasonCodeDto?>(http, "api/season-codes", dto, ct);

    private static void AddFile(MultipartFormDataContent form, string fieldName, Stream stream, string fileName, string? contentType)
    {
        var content = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }

        form.Add(content, fieldName, fileName);
    }
}
