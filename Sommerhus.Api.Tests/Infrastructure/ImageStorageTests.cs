using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Sommerhus.Api.Extensions;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Core.Services.Storage;

namespace Sommerhus.Api.Tests.Infrastructure;

public sealed class ImageStorageTests
{
    [Theory]
    [InlineData("")]
    [InlineData("/api")]
    public async Task FirstUpload_WithoutPublishedWebRoot_IsImmediatelyServed(string pathBase)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "sommerhus_image_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);
        try
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRoot,
                EnvironmentName = "Production"
            });
            builder.WebHost.UseTestServer();
            builder.Services.AddStorageServices(builder.Configuration);
            await using var app = builder.Build();

            app.Environment.WebRootFileProvider.Should().BeOfType<NullFileProvider>();
            app.UsePathBase(pathBase);
            app.UseImageFiles();
            app.MapPost("/upload", async (Microsoft.AspNetCore.Http.HttpRequest request, IImageStorage storage) =>
            {
                var form = await request.ReadFormAsync();
                var stored = await storage.SaveAsync(ImageCategory.Feature, Guid.NewGuid(), form.Files[0], default);
                return Microsoft.AspNetCore.Http.Results.Json(storage.GetUrl(request.BaseUrl(), stored.RelativePath));
            });
            await app.StartAsync();

            using var client = app.GetTestClient();
            using var content = new MultipartFormDataContent();
            var bytes = new byte[] { 1, 2, 3, 4 };
            using var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(file, "file", "icon.png");
            using var upload = await client.PostAsync($"{pathBase}/upload", content);
            upload.StatusCode.Should().Be(HttpStatusCode.OK);
            var url = await upload.Content.ReadFromJsonAsync<string>();
            url.Should().StartWith($"http://localhost{pathBase}/uploads/features/");

            using var image = await client.GetAsync(url);
            image.StatusCode.Should().Be(HttpStatusCode.OK);
            image.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
            (await image.Content.ReadAsByteArrayAsync()).Should().Equal(bytes);
            Directory.EnumerateFiles(Path.Combine(contentRoot, "wwwroot"), "*.png", SearchOption.AllDirectories)
                .Should().ContainSingle();
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
