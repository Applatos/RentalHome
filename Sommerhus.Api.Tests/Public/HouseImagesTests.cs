using FluentAssertions;
using Sommerhus.Api.Dtos.Public.Houses;
using Sommerhus.Api.Dtos.Shared;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Sommerhus.Api.Tests.Public;

public class HouseImagesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    public HouseImagesTests(CustomWebApplicationFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task UploadGalleryImage_ThenListShowsUrl()
    {
        // find et hus
        var houses = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        var houseId = houses!.First().Id;

        // lav multipart indhold med en fiktiv fil
        var content = new MultipartFormDataContent();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        var res = await _client.PostAsync($"/api/houses/{houseId}/images/gallery", content);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploaded = await res.Content.ReadFromJsonAsync<ImageDto>();
        uploaded!.Url.Should().StartWith("http");

        // list igen
        var list = await _client.GetFromJsonAsync<List<ImageDto>>($"/api/houses/{houseId}/images");
        list!.Any(i => i.Id == uploaded.Id).Should().BeTrue();
    }
}
