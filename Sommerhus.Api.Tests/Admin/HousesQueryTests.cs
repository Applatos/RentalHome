//using FluentAssertions;
//using Sommerhus.Contracts.Dtos._api.Houses;
//using Sommerhus.Contracts.Dtos.Shared;
//using Sommerhus.Api.Tests.Infrastructure;
//using System.Net.Http.Json;
//using Xunit.Abstractions;

//namespace Sommerhus.Api.Tests.Admin;

//public class HousesQueryTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;
//    private readonly ITestOutputHelper _out;

//    public HousesQueryTests(CustomWebApplicationFactory f, ITestOutputHelper o)
//    {
//        _client = f.CreateClient();
//        _out = o;
//    }

//    [Fact]
//    public async Task Pagination_NoOverlap()
//    {
//        var pagesize = 2;
//        var p1 = await _client.GetFromJsonAsync<PageResult<HouseListItemDto>>($"/api/admin/houses?page=1&pageSize={pagesize}");
//        var p2 = await _client.GetFromJsonAsync<PageResult<HouseListItemDto>>($"/api/admin/houses?page=2&pageSize={pagesize}");

//        if (p1!.Total > pagesize)
//        {
//            p2!.Items.Should().NotBeEmpty();
//            var ids1 = p1.Items.Select(i => i.Id).ToHashSet();
//            var ids2 = p2.Items.Select(i => i.Id).ToHashSet();
//            ids1.Intersect(ids2).Should().BeEmpty();
//        }
//        else
//        {
//            // hvis kun 2 eller færre i alt, giver tom side 2 mening
//            p2!.Items.Should().BeEmpty();
//        }
//    }

//    [Fact]
//    public async Task Query_Filters()
//    {
//        var res = await _client.GetFromJsonAsync<PageResult<HouseListItemDto>>("/api/admin/houses?query=Blå");
//        res!.Items.Should().OnlyContain(i =>
//            (i.City ?? "").Contains("Blå", StringComparison.OrdinalIgnoreCase) ||
//            (i.Title ?? "").Contains("Blå", StringComparison.OrdinalIgnoreCase));
//    }
//}
