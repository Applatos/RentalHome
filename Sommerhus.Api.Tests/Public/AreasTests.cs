using FluentAssertions;
using Sommerhus.Api.Dtos.Public.Areas;
using Sommerhus.Api.Tests.Infrastructure;
using System.Net.Http.Json;

namespace Sommerhus.Api.Tests.Public;

public class AreasTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AreasTests(CustomWebApplicationFactory f) => _client = f.CreateClient();

    [Fact]
    public async Task List_ReturnsCounts()
    {
        var data = await _client.GetFromJsonAsync<List<AreaDto>>("/api/areas");
        data.Should().NotBeNullOrEmpty();
        data!.First().Count.Should().BeGreaterThanOrEqualTo(0);
    }
}
