using Sommerhus.Mvc.Controllers;
using System.Globalization;
using System.Net.Http.Json;

namespace Sommerhus.Mvc.Services;

public interface ISommerhusApi
{
    // Houses
    Task<IReadOnlyList<HouseListItem>> GetHousesAsync(string? citySlug = null, string? q = null, CancellationToken ct = default);
    Task<HouseDetails?> GetHouseAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateHouseAsync(AdminController.CreateFormVM form, CancellationToken ct = default);
    Task UpdateHouseAsync(Guid id, AdminController.CreateFormVM form, CancellationToken ct = default);
    Task DeleteHouseAsync(Guid id, CancellationToken ct = default);

    // Images
    Task<(Guid id, string url)> UploadCoverAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default);
    Task<HouseImage> UploadGalleryImageAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default);
    Task<IReadOnlyList<HouseImage>> UploadGalleryAsync(Guid houseId, IEnumerable<(Stream stream, string fileName)> files, CancellationToken ct = default);
    Task<(Guid id, string url)> UploadFloorplanAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default);
    Task SetCoverAsync(Guid houseId, Guid imageId, CancellationToken ct = default);
    Task DeleteImageAsync(Guid houseId, Guid imageId, CancellationToken ct = default);

    // Features
    Task<IReadOnlyList<FeatureDto>> GetFeaturesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FeatureValueDto>> GetHouseFeaturesAsync(Guid houseId, CancellationToken ct = default);
    Task UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<CreateHouseFeatureValueDto> values, CancellationToken ct = default);

    // Cities / Zip
    Task<IReadOnlyList<CityListItem>> GetCitiesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ZipCodeDto>> SearchZipcodesAsync(string filter, CancellationToken ct = default);

    // Areas
    Task<IReadOnlyList<AreaListItem>> GetAreasAsync(string? query = null, CancellationToken ct = default);
    Task<AreaDetails?> GetAreaAsync(string slug, CancellationToken ct = default);
}

// DTOs (MVC side)
public record HouseListItem(Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? Cover);
public record HouseImage(Guid Id, string Url, string? Alt, string Kind);

public record FeatureDto(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl, int SortOrder);
public record FeatureValueDto(Guid FeatureId, string Name, string Key, string ValueType, string? Unit, string? IconUrl,
    bool? Bool, int? Int, decimal? Decimal, string? Text, string Display);
public record CreateHouseFeatureValueDto(Guid FeatureId, bool? ValueBool, int? ValueInt, decimal? ValueDecimal, string? ValueText);

public record HouseDetails(
    Guid Id,
    string Title,
    string? Subtitle,
    string? City,
    string? Zip,
    string? Address,
    string? CitySlug,
    string? Description,
    string? Facilities,
    string? CoverUrl,
    HouseImage[] Gallery,
    HouseImage? Floorplan,
    FeatureValueDto[] Features);

public record CityListItem(Guid Id, string Slug, string City, string Zip, int Count);
public record ZipCodeDto(string Zip, string City);

public record AreaListItem(Guid Id, string Slug, string Name, string? City, int HouseCount, string? Summary, string? HeroImageUrl);
public record AreaDetails(Guid Id, string Slug, string Name, string? City, string? Description, AreaImage[] Images, AreaHouse[] Houses);
public record AreaImage(Guid Id, string Url);
public record AreaHouse(Guid Id, string Title, string? Subtitle, string? City, string? Zip, string? CoverUrl);

public class SommerhusApi(HttpClient http) : ISommerhusApi
{
    // Houses
    public async Task<IReadOnlyList<HouseListItem>> GetHousesAsync(string? citySlug = null, string? q = null, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(citySlug)) qs.Add($"city={Uri.EscapeDataString(citySlug)}");
        if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
        var url = "api/houses" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await http.GetFromJsonAsync<List<HouseListItem>>(url, ct) ?? [];
    }

    public async Task<HouseDetails?> GetHouseAsync(Guid id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<HouseDetails>($"api/houses/{id}", ct);

    public async Task<Guid> CreateHouseAsync(AdminController.CreateFormVM form, CancellationToken ct = default)
    {
        var dto = new
        {
            form.Title,
            form.Subtitle,
            form.Address,
            CityId = form.CityId ?? throw new ArgumentException("CityId is required", nameof(form)),
            form.Description,
            form.Facilities
        };
        var resp = await http.PostAsJsonAsync("api/admin/houses", dto, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<Guid>(cancellationToken: ct);
    }

    public async Task UpdateHouseAsync(Guid id, AdminController.CreateFormVM form, CancellationToken ct = default)
    {
        var dto = new
        {
            form.Title,
            form.Subtitle,
            form.Address,
            CityId = form.CityId ?? throw new ArgumentException("CityId is required", nameof(form)),
            form.Description,
            form.Facilities
        };
        var resp = await http.PutAsJsonAsync($"api/admin/houses/{id}", dto, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteHouseAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await http.DeleteAsync($"api/admin/houses/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }

    // Images
    public async Task<(Guid id, string url)> UploadCoverAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var resp = await http.PostAsync($"api/admin/houses/{houseId}/images/cover", content, ct);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<HouseImage>(cancellationToken: ct)
                  ?? throw new InvalidOperationException("Missing cover image payload");
        return (dto.Id, dto.Url);
    }

    public async Task<HouseImage> UploadGalleryImageAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var resp = await http.PostAsync($"api/admin/houses/{houseId}/images/gallery", content, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<HouseImage>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Missing gallery image payload");
    }

    public async Task<IReadOnlyList<HouseImage>> UploadGalleryAsync(Guid houseId, IEnumerable<(Stream stream, string fileName)> files, CancellationToken ct = default)
    {
        var result = new List<HouseImage>();
        foreach (var (stream, fileName) in files)
        {
            var image = await UploadGalleryImageAsync(houseId, stream, fileName, ct);
            result.Add(image);
        }
        return result;
    }

    public async Task<(Guid id, string url)> UploadFloorplanAsync(Guid houseId, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);
        var resp = await http.PostAsync($"api/admin/houses/{houseId}/images/floorplan", content, ct);
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<HouseImage>(cancellationToken: ct)
                  ?? throw new InvalidOperationException("Missing floorplan payload");
        return (dto.Id, dto.Url);
    }

    public async Task SetCoverAsync(Guid houseId, Guid imageId, CancellationToken ct = default)
        => (await http.PostAsync($"api/admin/houses/{houseId}/images/{imageId}/set-cover", null, ct)).EnsureSuccessStatusCode();

    public async Task DeleteImageAsync(Guid houseId, Guid imageId, CancellationToken ct = default)
        => (await http.DeleteAsync($"api/admin/houses/{houseId}/images/{imageId}", ct)).EnsureSuccessStatusCode();

    // Features
    public async Task<IReadOnlyList<FeatureDto>> GetFeaturesAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<FeatureDto>>("api/features", ct) ?? [];

    public async Task<IReadOnlyList<FeatureValueDto>> GetHouseFeaturesAsync(Guid houseId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<FeatureValueDto>>($"api/houses/{houseId}/features", ct) ?? [];

    public async Task UpsertHouseFeaturesAsync(Guid houseId, IEnumerable<CreateHouseFeatureValueDto> values, CancellationToken ct =
default)
    {
        var items = values.Select(v =>
        {
            string rawValue;
            if (v.ValueBool.HasValue)
            {
                rawValue = v.ValueBool.Value ? "true" : "false";
            }
            else if (v.ValueInt.HasValue)
            {
                rawValue = v.ValueInt.Value.ToString(CultureInfo.InvariantCulture);
            }
            else if (v.ValueDecimal.HasValue)
            {
                rawValue = v.ValueDecimal.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                rawValue = v.ValueText ?? string.Empty;
            }

            return new { v.FeatureId, RawValue = rawValue };
        }).ToList();

        var resp = await http.PostAsJsonAsync($"api/admin/houses/{houseId}/features", items, ct);
        resp.EnsureSuccessStatusCode();
    }

    // Cities / Zip
    public async Task<IReadOnlyList<CityListItem>> GetCitiesAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<CityListItem>>("api/cities", ct) ?? [];

    public async Task<IReadOnlyList<ZipCodeDto>> SearchZipcodesAsync(string filter, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<ZipCodeDto>>($"api/zipcodes?filter={Uri.EscapeDataString(filter)}", ct) ?? [];

    public async Task<IReadOnlyList<AreaListItem>> GetAreasAsync(string? query = null, CancellationToken ct = default)
    {
        var url = "api/areas";
        if (!string.IsNullOrWhiteSpace(query))
        {
            url += $"?q={Uri.EscapeDataString(query)}";
        }

        return await http.GetFromJsonAsync<List<AreaListItem>>(url, ct) ?? [];
    }

    public async Task<AreaDetails?> GetAreaAsync(string slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required", nameof(slug));
        return await http.GetFromJsonAsync<AreaDetails>($"api/areas/{Uri.EscapeDataString(slug)}", ct);
    }
}
