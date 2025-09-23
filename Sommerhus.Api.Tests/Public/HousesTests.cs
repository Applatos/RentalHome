using FluentAssertions;
using Sommerhus.Api.Dtos.Public.Houses;
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
        await res.DumpIfError(_out);   // <- viser ProblemDetails JSON i test-output
        res.EnsureSuccessStatusCode();


        var data = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        data.Should().NotBeNull();
        data!.Count.Should().BeGreaterThan(0);
        // Cover kan være null – men hvis sat, skal det ligne en URL
        data!.Where(d => d.Cover is not null).All(d => d.Cover!.StartsWith("http")).Should().BeTrue();
    }

    [Fact]
    public async Task Detail_ReturnsGalleryAndFeatures()
    {
       
        var res = await _client.GetAsync("/api/houses?take=10");
        await res.DumpIfError(_out);   // <- viser ProblemDetails JSON i test-output
        res.EnsureSuccessStatusCode();

        var list = await _client.GetFromJsonAsync<List<HouseListItemDto>>("/api/houses?take=1");
        var id = list!.First().Id;

        var detail = await _client.GetFromJsonAsync<HouseDetailsDto>($"/api/houses/{id}");
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(id);
        detail.Gallery.Should().NotBeNull();
        detail.Features.Should().NotBeNull();
    }
}
