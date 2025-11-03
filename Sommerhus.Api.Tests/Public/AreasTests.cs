//using FluentAssertions;
//using Sommerhus.Contracts.Dtos.Public.Areas;
//using Sommerhus.Api.Tests.Infrastructure;
//using System.Net.Http.Json;

//namespace Sommerhus.Api.Tests.Public;

//public class AreasTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;

//    public AreasTests(CustomWebApplicationFactory f) => _client = f.CreateClient();

//    [Fact]
//    public async Task List_ReturnsCounts()
//    {
//        var data = await _client.GetFromJsonAsync<List<AreaListItemDto>>("/api/areas");
//        data.Should().NotBeNullOrEmpty();
//        data!.First().HouseCount.Should().BeGreaterThanOrEqualTo(0);
//        data.First().Slug.Should().NotBeNullOrWhiteSpace();
//    }

//    [Fact]
//    public async Task Detail_BySlug_ReturnsAreaWithImagesAndHouses()
//    {
//        var list = await _client.GetFromJsonAsync<List<AreaListItemDto>>("/api/areas");
//        list.Should().NotBeNullOrEmpty();

//        var slug = list!.First().Slug;
//        slug.Should().NotBeNullOrWhiteSpace();

//        var detail = await _client.GetFromJsonAsync<AreaDetailDto>($"/api/areas/{slug}");
//        detail.Should().NotBeNull();
//        detail!.Slug.Should().Be(slug);
//        detail.Images.Should().NotBeNull();
//        detail.Houses.Should().NotBeNull();
//    }
//}
