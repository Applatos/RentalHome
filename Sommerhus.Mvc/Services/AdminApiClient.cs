// Sommerhus.Mvc/Services/AdminApiClient.cs
using System.Net.Http.Json;

namespace Sommerhus.Mvc.Services;

public sealed class AdminApiClient
{
    private readonly HttpClient _http;
    public AdminApiClient(HttpClient http) => _http = http;

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

    public async Task<HousePage> SearchHousesAsync(string? q, int page, int pageSize, CancellationToken ct)
    {
        var url = $"api/admin/houses?query={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}";
        return await _http.GetFromJsonAsync<HousePage>(url, ct) ?? new HousePage { Query = q, Page = page, PageSize = pageSize, Total = 0, Items = new() };
    }

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
}
