using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.Services;
using MvcFeaturesController = Sommerhus.Mvc.Controllers.Admin.FeaturesController;

namespace Sommerhus.Api.Tests.Admin;

public sealed class FeatureIconTests : IDisposable
{
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jXioAAAAASUVORK5CYII=");
    private readonly IisApplicationFactory factory = new();
    private readonly HttpClient client;
    private readonly AdminApiClient api;

    public FeatureIconTests()
    {
        client = factory.CreateClient();
        client.BaseAddress = new Uri("http://localhost/api/");
        using var login = client.PostAsJsonAsync("api/admin/auth/login", new AdminLoginRequest
        {
            Username = "admin",
            Password = "Sommerhus123!"
        }).GetAwaiter().GetResult();
        login.EnsureSuccessStatusCode();
        var token = login.Content.ReadFromJsonAsync<AdminTokenResponse>().GetAwaiter().GetResult()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        api = new AdminApiClient(client);
    }

    [Fact]
    public async Task Upload_ThroughMvc_ReturnsReadableIconUnderIisApplicationPath()
    {
        var featureId = await CreateFeatureAsync();
        var controller = CreateController();
        using var stream = new MemoryStream(Png);
        var file = MakeFile(stream, "pool.png", "image/png");

        await controller.UploadIcon(featureId, file);

        controller.TempData["Ok"].Should().Be("Icon uploaded.");
        var features = await api.GetFeaturesAsync(CancellationToken.None);
        var url = features.Data!.Single(f => f.Id == featureId).IconUrl;
        url.Should().StartWith($"http://localhost/api/uploads/features/{featureId}/");

        using var publicClient = factory.CreateClient();
        using var image = await publicClient.GetAsync(url);
        image.StatusCode.Should().Be(HttpStatusCode.OK);
        image.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        (await image.Content.ReadAsByteArrayAsync()).Should().Equal(Png);
    }

    [Theory]
    [InlineData("pool.webp", "image/webp", 16, "Only PNG and JPEG are allowed.")]
    [InlineData("pool.png", "image/png", 2 * 1024 * 1024 + 1, "File is too large (max 2MB).")]
    public async Task Upload_ValidationFailure_ShowsApiReasonInMvc(
        string name, string contentType, int length, string expectedMessage)
    {
        var featureId = await CreateFeatureAsync();
        var controller = CreateController();
        using var stream = new MemoryStream(new byte[length]);

        await controller.UploadIcon(featureId, MakeFile(stream, name, contentType));

        controller.TempData["Err"].Should().Be(expectedMessage);
        controller.TempData.ContainsKey("Ok").Should().BeFalse();
        var features = await api.GetFeaturesAsync(CancellationToken.None);
        features.Data!.Single(f => f.Id == featureId).IconUrl.Should().BeNull();
    }

    private async Task<Guid> CreateFeatureAsync()
    {
        var result = await api.CreateFeatureAsync(
            new UpsertFeatureDto("Pool", null, $"pool_{Guid.NewGuid():N}", "Bool"),
            CancellationToken.None);
        result.Ok.Should().BeTrue();
        return result.Data;
    }

    private MvcFeaturesController CreateController()
        => new(api)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new EmptyTempDataProvider())
        };

    private static FormFile MakeFile(Stream stream, string name, string contentType)
        => new(stream, 0, stream.Length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    private sealed class IisApplicationFactory : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, IisPathBase>());
        }
    }

    private sealed class IisPathBase : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.UsePathBase("/api");
                next(app);
            };
    }
}
