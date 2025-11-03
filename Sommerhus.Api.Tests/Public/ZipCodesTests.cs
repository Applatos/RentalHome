//using FluentAssertions;
//using Sommerhus.Api.Tests.Infrastructure;
//using System.Net.Http.Json;

//namespace Sommerhus.Api.Tests.Public;

//public class ZipCodesTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;
//    public ZipCodesTests(CustomWebApplicationFactory factory) => _client = factory.CreateClient();

//    [Fact]
//    public async Task Search_ReturnsMatchesAndLimits()
//    {
//        var all = await _client.GetFromJsonAsync<List<ZipCodeDto>>("/api/zipcodes");
//        all.Should().NotBeNull();
//        all!.Count.Should().BeGreaterThan(0);

//        var filtered = await _client.GetFromJsonAsync<List<ZipCodeDto>>("/api/zipcodes?filter=685");
//        filtered.Should().NotBeNull();
//        filtered!.All(z => z.Zip.Contains("685")).Should().BeTrue();
//    }

//    private record ZipCodeDto(string Zip, string City);
//}
