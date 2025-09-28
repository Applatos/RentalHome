using FluentAssertions;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Sommerhus.Api.Tests.Admin;

public class HouseImagesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public HouseImagesTests(CustomWebApplicationFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task UploadSetCoverAndDeleteLifecycle()
    {
        var houses = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        var houseId = houses!.First().Id;

        using var content = new MultipartFormDataContent();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var upload = await _client.PostAsync($"/api/admin/houses/{houseId}/images/gallery", content);
        upload.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploaded = await upload.Content.ReadFromJsonAsync<ImageDto>();
        uploaded.Should().NotBeNull();

        var setCover = await _client.PostAsync($"/api/admin/houses/{houseId}/images/{uploaded!.Id}/set-cover", null);
        setCover.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<List<ImageDto>>($"/api/admin/houses/{houseId}/images");
        list.Should().Contain(i => i.Id == uploaded.Id && string.Equals(i.Kind, "Cover", StringComparison.OrdinalIgnoreCase));

        var delete = await _client.DeleteAsync($"/api/admin/houses/{houseId}/images/{uploaded.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await _client.GetFromJsonAsync<List<ImageDto>>($"/api/admin/houses/{houseId}/images");
        afterDelete.Should().NotContain(i => i.Id == uploaded.Id);
    }
}
