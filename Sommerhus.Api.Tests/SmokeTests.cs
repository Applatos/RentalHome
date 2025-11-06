//using System.Net;
//using FluentAssertions;
//using Sommerhus.Api.Tests.Infrastructure;

//namespace Sommerhus.Api.Tests;

//public class SmokeTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;

//    public SmokeTests(CustomWebApplicationFactory factory)
//    {
//        _client = factory.CreateClient();
//    }

//    [Fact]
//    public async Task SwaggerEndpoint_ReturnsOk()
//    {
//        var response = await _client.GetAsync("/swagger/v1/swagger.json");
//        response.StatusCode.Should().Be(HttpStatusCode.OK);
//    }
//}