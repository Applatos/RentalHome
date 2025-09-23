using FluentAssertions;
using Sommerhus.Api.Dtos.Admin.Cities;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public class CitiesCrudTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _out;

    public CitiesCrudTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _out = output;
    }

    [Fact]
    public async Task CityLifecycle_WithImages_Works()
    {
        var slug = $"test-{Guid.NewGuid():N}";
        var create = new CreateCityDto("Test City", "9999", slug, "En test" );
        var res = await _client.PostAsJsonAsync("/api/admin/cities", create);
        await res.DumpIfError(_out);
        res.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await res.Content.ReadFromJsonAsync<Guid>();

        var detail = await _client.GetFromJsonAsync<CityDetailDto>($"/api/admin/cities/{id}");
        detail.Should().NotBeNull();
        detail!.Slug.Should().Be(slug);
        detail.Text.Should().Be("En test");

        var update = new UpdateCityDto("Updated City", "9998", slug, "Ny tekst");
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/cities/{id}", update);
        await updateRes.DumpIfError(_out);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updated = await _client.GetFromJsonAsync<CityDetailDto>($"/api/admin/cities/{id}");
        updated!.Name.Should().Be("Updated City");
        updated.Zip.Should().Be("9998");
        updated.Text.Should().Be("Ny tekst");

        using var content = new MultipartFormDataContent();
        var bytes = new byte[] { 1, 2, 3 };
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "city.png");

        var upload = await _client.PostAsync($"/api/admin/cities/{id}/images", content);
        upload.StatusCode.Should().Be(HttpStatusCode.Created);

        var withImage = await _client.GetFromJsonAsync<CityDetailDto>($"/api/admin/cities/{id}");
        withImage!.Images.Should().HaveCount(1);

        var imageId = withImage.Images.First().Id;
        var deleteImage = await _client.DeleteAsync($"/api/admin/cities/{id}/images/{imageId}");
        deleteImage.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await _client.GetFromJsonAsync<CityDetailDto>($"/api/admin/cities/{id}");
        afterDelete!.Images.Should().BeEmpty();

        var deleteCity = await _client.DeleteAsync($"/api/admin/cities/{id}");
        deleteCity.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterCityDelete = await _client.GetAsync($"/api/admin/cities/{id}");
        afterCityDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DuplicateSlug_ReturnsValidationError()
    {
        var slug = $"dup-{Guid.NewGuid():N}";
        var payload = new CreateCityDto("City", "1000", slug, null);
        var first = await _client.PostAsJsonAsync("/api/admin/cities", payload);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/admin/cities", payload);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
