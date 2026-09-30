using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Mvc.ModelBinding;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>The MVC site's shell: assets, layout, number binding.</summary>
public sealed class ShellTests(MvcAppFixture app) : IClassFixture<MvcAppFixture>
{
    [Theory]
    [InlineData("/lib/bootstrap/dist/css/bootstrap.min.css", "text/css", "Bootstrap  v5.3.3")]
    [InlineData("/lib/bootstrap/dist/js/bootstrap.bundle.min.js", "text/javascript", "Bootstrap v5.3.3")]
    [InlineData("/lib/bootstrap-icons/font/bootstrap-icons.min.css", "text/css", "Bootstrap Icons v1.11.3")]
    public async Task LibraryAssets_AreServedLocally(string path, string contentType, string banner)
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await browser.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(contentType);
        (await response.Content.ReadAsStringAsync()).Should().Contain(banner);
    }

    [Theory]
    [InlineData("/lib/bootstrap-icons/font/fonts/bootstrap-icons.woff2", "font/woff2")]
    [InlineData("/lib/bootstrap-icons/font/fonts/bootstrap-icons.woff", "application/font-woff")]
    public async Task IconFonts_AreServedLocally(string path, string contentType)
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await browser.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(contentType);
    }

    [Fact]
    public async Task Layout_LoadsBootstrapAndIcons()
    {
        using var browser = app.Mvc.CreateBrowser();

        var html = await browser.GetStringAsync("/account/login");

        html.Should().Contain("href=\"/lib/bootstrap/dist/css/bootstrap.min.css?v=");
        html.Should().Contain("href=\"/lib/bootstrap-icons/font/bootstrap-icons.min.css?v=");
    }

    [Theory]
    [InlineData("da-DK", "Filtre")]
    [InlineData("en-GB", "Filters")]
    public async Task HouseList_FilterToggle_HasIconLabelAndTarget(string culture, string label)
    {
        using var browser = app.Mvc.CreateBrowser();

        var html = await browser.GetStringAsync($"/houses?culture={culture}");

        html.Should().Contain("aria-controls=\"advancedSearch\"");
        html.Should().Contain($"<i class=\"bi bi-sliders me-1\" aria-hidden=\"true\"></i>{label}");
    }

    [Fact]
    public void NumberBinderProvider_IsRegisteredFirst()
    {
        var options = app.Mvc.Services.GetRequiredService<IOptions<MvcOptions>>().Value;

        options.ModelBinderProviders[0].Should().BeOfType<CultureSafeNumberModelBinderProvider>();
    }

    [Theory]
    [InlineData(typeof(decimal), null, true)]
    [InlineData(typeof(decimal?), "Form", true)]
    [InlineData(typeof(double), "Query", true)]
    [InlineData(typeof(float?), "Path", true)]
    [InlineData(typeof(decimal), "Body", false)]
    [InlineData(typeof(decimal), "Header", false)]
    [InlineData(typeof(int), "Form", false)]
    public void MvcBinderFactory_UsesTheCultureSafeBinder_ForValueProviderNumbersOnly(
        Type type, string? source, bool expected)
    {
        var factory = app.Mvc.Services.GetRequiredService<IModelBinderFactory>();
        var metadata = app.Mvc.Services.GetRequiredService<IModelMetadataProvider>().GetMetadataForType(type);
        var bindingInfo = new BindingInfo
        {
            BindingSource = source switch
            {
                "Form" => BindingSource.Form,
                "Query" => BindingSource.Query,
                "Path" => BindingSource.Path,
                "Body" => BindingSource.Body,
                "Header" => BindingSource.Header,
                _ => null
            }
        };

        var binder = factory.CreateBinder(new ModelBinderFactoryContext
        {
            Metadata = metadata,
            BindingInfo = bindingInfo,
            CacheToken = new object()
        });

        (binder is CultureSafeNumberModelBinder).Should().Be(expected, "binder was {0}", binder.GetType().Name);
    }

    [Fact]
    public async Task HouseFeatures_DecimalValuesPostedUnderDanishCulture_AreStoredAsTyped()
    {
        using var api = app.Api.CreateAuthenticatedClient();
        var houseId = await CreateHouseAsync(api);
        var browserFeature = await CreateDecimalFeatureAsync(api, "Grundareal");
        var typedFeature = await CreateDecimalFeatureAsync(api, "Afstand");

        using var admin = await app.SignInAsync(MvcAppFixture.AdminName);
        var token = await MvcAppFixture.GetAntiforgeryTokenAsync(admin, "/account/access-denied");
        using var response = await admin.PostAsync($"/admin/houses/{houseId}/features", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                // What <input type="number"> posts, and what a Dane types into a text field.
                [$"feature_{browserFeature}"] = "1.5",
                [$"feature_{typedFeature}"] = "2,75",
                ["__RequestVerificationToken"] = token
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be($"/admin/houses/{houseId}?tab=features");
        var house = await api.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        house!.Features.Single(f => f.Id == browserFeature).RawValue.Should().Be("1.5");
        house.Features.Single(f => f.Id == typedFeature).RawValue.Should().Be("2.75");
    }

    private static async Task<Guid> CreateHouseAsync(HttpClient api)
    {
        var cities = await api.GetFromJsonAsync<List<CityDto>>("api/cities");
        using var response = await api.PostAsJsonAsync("api/admin/houses", new UpsertHouseDto
        {
            Title = $"Culture house {Guid.NewGuid():N}",
            Address = "Strandvej 1",
            CityId = cities!.First().Id,
            Description = "A house for the number binding test."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    private static async Task<Guid> CreateDecimalFeatureAsync(HttpClient api, string name)
    {
        using var response = await api.PostAsJsonAsync("api/admin/features",
            new UpsertFeatureDto(name, null, $"culture_{Guid.NewGuid():N}"[..30], "Decimal"));
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
