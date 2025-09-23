// Sommerhus.Mvc/Services/AdminApiClient.cs
using System.Collections.Generic;
using System.Net.Http.Json;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiClient
{
    private readonly HttpClient _http;
    public AdminApiClient(HttpClient http) => _http = http;

    // ---------- Shared DTO helpers ----------
    public sealed record LookupItem(Guid Id, string Label);

    // ---------- Houses (paged master) ----------
    public sealed record HouseListItem(Guid Id, string Title, string? City, string? Zip, string? Cover);
    public sealed class HousePage
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<HouseListItem> Items { get; set; } = new();
    }

    public sealed record HouseDetails(
        Guid Id,
        string Title,
        string? Subtitle,
        string? Address,
        Guid CityId,
        string? Description,
        string? Facilities,
        Guid? AreaId,
        Guid? CoverImageId);

    public async Task<HousePage> SearchHousesAsync(string? q, int page, int pageSize, CancellationToken ct)
    {
        var url = $"api/admin/houses?query={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<HousePage>(url, ct) ?? new HousePage { Query = q, Page = page, PageSize = pageSize, Total = 0, Items = new() };
    }

    public Task<HouseDetails?> GetHouseAsync(Guid id, CancellationToken ct)
        => _http.GetFromJsonAsync<HouseDetails>($"api/admin/houses/{id}", ct);

    // ---------- Cities (områder) ----------
    public sealed record CityListItem(Guid Id, string Name, string Zip, string? Slug);
    public sealed class CityPage
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<CityListItem> Items { get; set; } = new();
    }

    public sealed record CityDetails(Guid Id, string Name, string Zip, string? Slug, string? Text, string[] Images);

    public Task<CityPage?> ListCitiesAsync(string? q, int page, int pageSize, CancellationToken ct)
        => _http.GetFromJsonAsync<CityPage>($"api/admin/cities?q={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}", ct);

    public Task<CityDetails?> GetCityAsync(Guid id, CancellationToken ct)
        => _http.GetFromJsonAsync<CityDetails>($"api/admin/cities/{id}", ct);

    public async Task<Guid> CreateCityAsync(string name, string zip, string? slug, string? text, CancellationToken ct)
    {
        var res = await _http.PostAsJsonAsync("api/admin/cities", new { name, zip, slug, text }, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task UpdateCityAsync(Guid id, string name, string zip, string? slug, string? text, CancellationToken ct)
    {
        var res = await _http.PutAsJsonAsync($"api/admin/cities/{id}", new { name, zip, slug, text }, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteCityAsync(Guid id, CancellationToken ct)
    {
        var res = await _http.DeleteAsync($"api/admin/cities/{id}", ct);
        res.EnsureSuccessStatusCode();
    }

    // Billeder for City
    public async Task UploadCityImageAsync(Guid cityId, Stream fileStream, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var res = await _http.PostAsync($"api/admin/cities/{cityId}/images", content, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteCityImageAsync(Guid cityId, Guid imageId, CancellationToken ct)
    {
        var res = await _http.DeleteAsync($"api/admin/cities/{cityId}/images/{imageId}", ct);
        res.EnsureSuccessStatusCode();
    }

    // ---------- Zip lookup (typeahead) ----------
    public sealed record ZipListItem(Guid Id, string Zip, string City)
    {
        public string Display => string.IsNullOrWhiteSpace(Zip) ? City : $"{Zip} {City}".Trim();
    }

    public sealed class ZipPage
    {
        public string? Query { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ZipListItem> Items { get; set; } = new();
    }

    public async Task<ZipPage> SearchZipcodesAsync(string query, int page, int pageSize, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new ZipPage { Query = query, Page = page, PageSize = pageSize, Items = new List<ZipListItem>() };
        }

        var url = $"api/admin/zipcodes?query={Uri.EscapeDataString(query)}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<ZipPage>(url, ct) ?? new ZipPage { Query = query, Page = page, PageSize = pageSize };
    }

    // ---------- Areas ----------
    public sealed record AreaListItem(Guid Id, string Name, int HouseCount, int ImageCount);
    public sealed record AreaImage(Guid Id, string Url);
    public sealed record AreaDetails(Guid Id, string Name, string? Description, List<AreaImage> Images);

    public async Task<IReadOnlyList<AreaListItem>> GetAreasAsync(CancellationToken ct)
        => await _http.GetFromJsonAsync<List<AreaListItem>>("api/admin/areas", ct) ?? new();

    public Task<AreaDetails?> GetAreaAsync(Guid id, CancellationToken ct)
        => _http.GetFromJsonAsync<AreaDetails>($"api/admin/areas/{id}", ct);

    public async Task<Guid> CreateAreaAsync(string name, string? description, Guid? cityId, CancellationToken ct)
    {
        var payload = new { Name = name, Description = description, CityId = cityId, Images = Array.Empty<string>() };
        var res = await _http.PostAsJsonAsync("api/admin/areas", payload, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task UpdateAreaAsync(Guid id, string name, string? description, Guid? cityId, CancellationToken ct)
    {
        var payload = new { Name = name, Description = description, CityId = cityId, Images = Array.Empty<string>() };
        var res = await _http.PutAsJsonAsync($"api/admin/areas/{id}", payload, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAreaAsync(Guid id, CancellationToken ct)
    {
        var res = await _http.DeleteAsync($"api/admin/areas/{id}", ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task UploadAreaImageAsync(Guid areaId, Stream fileStream, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var res = await _http.PostAsync($"api/admin/areas/{areaId}/images", content, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAreaImageAsync(Guid areaId, Guid imageId, CancellationToken ct)
    {
        var res = await _http.DeleteAsync($"api/admin/areas/{areaId}/images/{imageId}", ct);
        res.EnsureSuccessStatusCode();
    }

    // ---------- Features ----------
    public sealed record FeatureListItem(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);

    public Task<List<FeatureListItem>?> GetFeaturesAsync(CancellationToken ct)
        => _http.GetFromJsonAsync<List<FeatureListItem>>("api/admin/features", ct);

    public async Task<Guid> CreateFeatureAsync(string name, string key, string valueType, string? unit, string? iconUrl, int sortOrder, CancellationToken ct)
    {
        var payload = new { Name = name, Key = key, ValueType = valueType, Unit = unit, IconUrl = iconUrl, SortOrder = sortOrder };
        var res = await _http.PostAsJsonAsync("api/admin/features", payload, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task UpdateFeatureAsync(Guid id, string name, string key, string valueType, string? unit, string? iconUrl, int sortOrder, CancellationToken ct)
    {
        var payload = new { Name = name, Key = key, ValueType = valueType, Unit = unit, IconUrl = iconUrl, SortOrder = sortOrder };
        var res = await _http.PutAsJsonAsync($"api/admin/features/{id}", payload, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteFeatureAsync(Guid id, CancellationToken ct)
    {
        var res = await _http.DeleteAsync($"api/admin/features/{id}", ct);
        res.EnsureSuccessStatusCode();
    }
}
