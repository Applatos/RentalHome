using FluentAssertions;
using Sommerhus.Api.Dtos.Public.Cities;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net.Http.Json;

namespace Sommerhus.Api.Tests.Public;

public class CitiesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CitiesTests(CustomWebApplicationFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task List_ReturnsSlugAndCounts()
    {
        var data = await _client.GetFromJsonAsync<List<CityListItemDto>>("/api/cities");
        data.Should().NotBeNullOrEmpty();
        data!.All(c => !string.IsNullOrWhiteSpace(c.Slug)).Should().BeTrue();
        data!.First().Count.Should().BeGreaterThanOrEqualTo(0);
    }
}
