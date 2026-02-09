using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Domain.Models;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiClient(HttpClient http)
{
    // Houses
    public Task<ApiResponse<PageResult<AdminHouseListItemDto>?>> GetHousesAsync(string? q, EntityStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var url = $"api/admin/houses?query={Uri.EscapeDataString(q ?? string.Empty)}&page={page}&pageSize={pageSize}";
        if (status.HasValue)
            url += $"&status={status.Value}";
        return ApiHttp.GetAsync<PageResult<AdminHouseListItemDto>>(http, url, ct);
    }

    public Task<ApiResponse<AdminHouseDetailsDto?>> GetHouseAsync(Guid id, CancellationToken ct)
        => ApiHttp.GetAsync<AdminHouseDetailsDto?>(http, $"api/admin/houses/{id}", ct);

    public Task<ApiResponse<Guid>> PostHouseAsync(UpsertHouseDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertHouseDto, Guid>(http, "api/admin/houses", dto, ct);

    public Task<ApiResponse<object?>> PutHouseAsync(Guid id, UpsertHouseDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertHouseDto, object?>(http, $"api/admin/houses/{id}", dto, ct);

    public Task<ApiResponse<object?>> DeleteHouseAsync(Guid id, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{id}", ct);

    public Task<ApiResponse<object?>> ChangeHouseStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<ChangeStatusDto, object?>(http, $"api/admin/houses/{id}/status", dto, ct);

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
    public Task<ApiResponse<IReadOnlyList<FeatureDto>?>> GetFeaturesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<FeatureDto>?>(http, "api/admin/features", ct);

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

    public Task<ApiResponse<object?>> ChangeAreaStatusAsync(Guid id, ChangeStatusDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<ChangeStatusDto, object?>(http, $"api/admin/areas/{id}/status", dto, ct);

    public Task<ApiResponse<IReadOnlyList<LookupItem>?>> GetAreasLookupAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<LookupItem>?>(http, "api/admin/areas/lookup", ct);

    public Task<ApiResponse<IReadOnlyList<ImageDto>?>> GetAreaImagesAsync(Guid areaId, CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<ImageDto>?>(http, $"api/admin/areas/{areaId}/images", ct);

    public async Task<ApiResponse<IReadOnlyList<ImageDto>?>> UploadAreaImagesAsync(Guid areaId, IEnumerable<IFormFile> files, CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            AddFile(form, "files", file.OpenReadStream(), file.FileName, file.ContentType);
        }

        return await ApiHttp.SendAsync<IReadOnlyList<ImageDto>?>(http, (client, token) => client.PostAsync($"api/admin/areas/{areaId}/images", form, token), ct);
    }

    public Task<ApiResponse<object?>> DeleteAreaImageAsync(Guid areaId, Guid imageId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/areas/{areaId}/images/{imageId}", ct);

    // Pricing (price plans)
    public Task<ApiResponse<PricePlanDetailsDto?>> PutHousePricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<PricePlanDetailsDto, PricePlanDetailsDto?>(http, $"api/admin/houses/{houseId}/pricing", dto, ct);

    public Task<ApiResponse<object?>> DeleteHouseRatePlanAsync(Guid houseId, Guid ratePlanId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{houseId}/pricing/rate-plans/{ratePlanId}", ct);

    // House groups
    public Task<ApiResponse<IReadOnlyList<HouseGroupDto>?>> GetHouseGroupListAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<HouseGroupDto>?>(http, "api/admin/house-groups", ct);

    public Task<ApiResponse<HouseGroupDto?>> GetHouseGroupAsync(Guid id, CancellationToken ct)
        => ApiHttp.GetAsync<HouseGroupDto?>(http, $"api/admin/house-groups/{id}", ct);

    public Task<ApiResponse<LookupItem?>> CreateHouseGroupAsync(HouseGroupDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<HouseGroupDto, LookupItem?>(http, "api/admin/house-groups", dto, ct);

    public Task<ApiResponse<HouseGroupDto?>> UpdateHouseGroupAsync(Guid id, UpsertHouseGroupDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertHouseGroupDto, HouseGroupDto?>(http, $"api/admin/house-groups/{id}", dto, ct);

    public Task<ApiResponse<object?>> DeleteHouseGroupAsync(Guid id, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/house-groups/{id}", ct);

    // Season spans for house groups
    public Task<ApiResponse<SeasonSpanDto?>> AddSeasonSpanAsync(Guid groupId, UpsertSeasonSpanDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertSeasonSpanDto, SeasonSpanDto?>(http, $"api/admin/house-groups/{groupId}/calendar", dto, ct);

    public Task<ApiResponse<SeasonSpanDto?>> UpdateSeasonSpanAsync(Guid groupId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertSeasonSpanDto, SeasonSpanDto?>(http, $"api/admin/house-groups/{groupId}/calendar/{spanId}", dto, ct);

    public Task<ApiResponse<object?>> DeleteSeasonSpanAsync(Guid groupId, Guid spanId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/house-groups/{groupId}/calendar/{spanId}", ct);

    // House season spans (when house has a group)
    public Task<ApiResponse<SeasonSpanDto?>> AddHouseSeasonSpanAsync(Guid houseId, UpsertSeasonSpanDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<UpsertSeasonSpanDto, SeasonSpanDto?>(http, $"api/admin/houses/{houseId}/calendar", dto, ct);

    public Task<ApiResponse<SeasonSpanDto?>> UpdateHouseSeasonSpanAsync(Guid houseId, Guid spanId, UpsertSeasonSpanDto dto, CancellationToken ct)
        => ApiHttp.PutAsync<UpsertSeasonSpanDto, SeasonSpanDto?>(http, $"api/admin/houses/{houseId}/calendar/{spanId}", dto, ct);

    public Task<ApiResponse<object?>> DeleteHouseSeasonSpanAsync(Guid houseId, Guid spanId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{houseId}/calendar/{spanId}", ct);

    // Calendars
    public Task<ApiResponse<IReadOnlyList<CalendarDto>?>> GetCalendarsAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<CalendarDto>?>(http, "api/admin/calendars", ct);

    public Task<ApiResponse<object?>> SetHouseCalendarOverrideAsync(Guid houseId, Guid calendarId, CancellationToken ct)
        => ApiHttp.PostAsync<SetCalendarOverrideDto, object?>(http, $"api/admin/houses/{houseId}/calendar-override", new SetCalendarOverrideDto { CalendarId = calendarId }, ct);

    public Task<ApiResponse<object?>> RemoveHouseCalendarOverrideAsync(Guid houseId, CancellationToken ct)
        => ApiHttp.DeleteAsync(http, $"api/admin/houses/{houseId}/calendar-override", ct);

    public Task<ApiResponse<CalendarDto?>> CreateHouseCalendarOverrideAsync(Guid houseId, string name, CancellationToken ct)
        => ApiHttp.PostAsync<CreateCalendarOverrideDto, CalendarDto?>(http, $"api/admin/houses/{houseId}/calendar-override/create", new CreateCalendarOverrideDto { Name = name }, ct);

    // Season Codes
    public Task<ApiResponse<IReadOnlyList<SeasonCodeDto>?>> GetSeasonCodesAsync(CancellationToken ct)
        => ApiHttp.GetAsync<IReadOnlyList<SeasonCodeDto>?>(http, "api/admin/pricing/season-codes", ct);

    public Task<ApiResponse<SeasonCodeDto?>> CreateSeasonCodeAsync(SeasonCodeDto dto, CancellationToken ct)
        => ApiHttp.PostAsync<SeasonCodeDto, SeasonCodeDto?>(http, "api/admin/pricing/season-codes", dto, ct);

    // Audit
    public Task<ApiResponse<PageResult<AuditEntryDto>?>> GetAuditEntriesAsync(string? entityType, string? entityId, int page, int pageSize, CancellationToken ct)
        => ApiHttp.GetAsync<PageResult<AuditEntryDto>?>(http, $"api/admin/audit?entity={Uri.EscapeDataString(entityType ?? "")}&entityId={Uri.EscapeDataString(entityId ?? "")}&page={page}&pageSize={pageSize}", ct);

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
