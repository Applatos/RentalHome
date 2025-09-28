using FluentAssertions;
using Sommerhus.Contracts.Dtos.Public.Houses;
using Sommerhus.Contracts.Dtos.Public.Cities;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net.Http.Json;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Public;

public class HousesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _out;

    public HousesTests(CustomWebApplicationFactory f, ITestOutputHelper o)
    {
        _client = f.CreateClient();
        _out = o;
    }

    [Fact]
    public async Task List_ReturnsWithCoverUrls()
    {
        var res = await _client.GetAsync("/api/houses?take=10");
        await res.DumpIfError(_out);
        res.EnsureSuccessStatusCode();

        var data = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=5");
        data.Should().NotBeNull();
        data!.Count.Should().BeGreaterThan(0);
        data.Where(d => d.Cover is not null).All(d => d.Cover!.StartsWith("http")).Should().BeTrue();
    }

    [Fact]
    public async Task Detail_ReturnsGalleryFeaturesAndCity()
    {
        var list = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        var id = list!.First().Id;

        var detail = await _client.GetFromJsonAsync<HouseDetailsDto>($"/api/houses/{id}");
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(id);
        detail.City.Should().NotBeNullOrWhiteSpace();
        detail.Zip.Should().NotBeNullOrWhiteSpace();
        detail.Gallery.Should().NotBeNull();
        detail.Features.Should().NotBeNull();
    }

    [Fact]
    public async Task Filter_ByCitySlug_ReturnsOnlyMatches()
    {
        var cities = await _client.GetFromJsonAsync<List<CityListItemDto>>("/api/cities");
        var slug = cities!.First().Slug;

        var data = await _client.GetFromJsonAsync<List<HouseListItemDto>>($"/api/houses?city={slug}");
        data.Should().NotBeNull();
        data!.All(h => string.Equals(h.City, cities.First(c => c.Slug == slug).City, StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
    }
}
