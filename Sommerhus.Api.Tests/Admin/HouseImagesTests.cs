using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Admin;

public class HouseImagesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;
    private readonly IWebHostEnvironment environment;

    public HouseImagesTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    }

    [Fact]
    public async Task UploadSetCoverAndDeleteLifecycle()
    {
        var houseId = await GetExistingHouseIdAsync();
        var uploaded = await UploadAsync(houseId, ("test.jpg", "image/jpeg"));
        var image = uploaded.Single();

        var setCover = await SetKindAsync(houseId, image.Id, ImageKind.Cover);
        setCover.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await GetImagesAsync(houseId);
        list.Should().Contain(i => i.Id == image.Id && string.Equals(i.Kind, nameof(ImageKind.Cover), StringComparison.OrdinalIgnoreCase));

        var delete = await client.DeleteAsync($"/api/admin/houses/{houseId}/images/{image.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await GetImagesAsync(houseId);
        afterDelete.Should().NotContain(i => i.Id == image.Id);
    }

    [Fact]
    public async Task Delete_RemovesFileFromStorage()
    {
        var houseId = await GetExistingHouseIdAsync();
        var uploaded = await UploadAsync(houseId, ("delete-me.jpg", "image/jpeg"));
        var image = uploaded.Single();

        var physicalPath = ResolvePhysicalPath(image.Url);
        File.Exists(physicalPath).Should().BeTrue("image should exist on disk after upload");

        var delete = await client.DeleteAsync($"/api/admin/houses/{houseId}/images/{image.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        File.Exists(physicalPath).Should().BeFalse("image should be removed from disk");
    }

    [Fact]
    public async Task SetKind_EnforcesExclusiveCoverAndFloorplan()
    {
        var houseId = await GetExistingHouseIdAsync();
        var uploads = await UploadAsync(
            houseId,
            ("cover-a.jpg", "image/jpeg"),
            ("cover-b.jpg", "image/jpeg"),
            ("plan-a.jpg", "image/jpeg"));

        var coverA = uploads[0];
        var coverB = uploads[1];
        var planA = uploads[2];

        (await SetKindAsync(houseId, coverA.Id, ImageKind.Cover)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SetKindAsync(houseId, coverB.Id, ImageKind.Cover)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SetKindAsync(houseId, planA.Id, ImageKind.Floorplan)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SetKindAsync(houseId, coverA.Id, ImageKind.Floorplan)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var images = await GetImagesAsync(houseId);
        var covers = images.Where(i => string.Equals(i.Kind, nameof(ImageKind.Cover), StringComparison.OrdinalIgnoreCase)).ToList();
        covers.Should().ContainSingle();
        covers.Single().Id.Should().Be(coverB.Id);

        var floorplans = images.Where(i => string.Equals(i.Kind, nameof(ImageKind.Floorplan), StringComparison.OrdinalIgnoreCase)).ToList();
        floorplans.Should().ContainSingle();
        floorplans.Single().Id.Should().Be(coverA.Id);
    }

    private async Task<Guid> GetExistingHouseIdAsync()
    {
        var houses = await client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        houses.Should().NotBeNull();
        houses!.Should().NotBeEmpty();
        return houses.First().Id;
    }

    private async Task<List<ImageDto>> UploadAsync(Guid houseId, params (string FileName, string ContentType)[] files)
    {
        using var content = new MultipartFormDataContent();
        foreach (var (fileName, contentType) in files)
        {
            var bytes = new byte[] { 1, 2, 3, 4, 5 };
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "files", fileName);
        }

        var response = await client.PostAsync($"/api/admin/houses/{houseId}/images", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploaded = await response.Content.ReadFromJsonAsync<List<ImageDto>>();
        uploaded.Should().NotBeNull();
        return uploaded!;
    }

    private async Task<List<ImageDto>> GetImagesAsync(Guid houseId)
    {
        var images = await client.GetFromJsonAsync<List<ImageDto>>($"/api/admin/houses/{houseId}/images");
        images.Should().NotBeNull();
        return images!;
    }

    private Task<HttpResponseMessage> SetKindAsync(Guid houseId, Guid imageId, ImageKind kind)
        => client.PostAsync($"/api/admin/houses/{houseId}/images/{imageId}/set-kind?kind={kind}", null);

    private string ResolvePhysicalPath(string imageUrl)
    {
        var uri = new Uri(imageUrl);
        var relative = uri.AbsolutePath.TrimStart('/');
        return Path.Combine(environment.WebRootPath!, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
