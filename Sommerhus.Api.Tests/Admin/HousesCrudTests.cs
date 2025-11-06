//using FluentAssertions;
//using Sommerhus.Contracts.Dtos._api.Cities;
//using Sommerhus.Contracts.Dtos._api.Features;
//using Sommerhus.Contracts.Dtos._api.Houses;
//using Sommerhus.Api.Tests.Infrastructure;
//using System.Net;
//using System.Net.Http.Json;
//using Xunit.Abstractions;

//namespace Sommerhus.Api.Tests.Admin;

//public class HousesCrudTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;
//    private readonly ITestOutputHelper _out;

//    public HousesCrudTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
//    {
//        _client = factory.CreateClient();
//        _out = output;
//    }

//    [Fact]
//    public async Task CreateUpdateDeleteLifecycle()
//    {
//        var cities = await _client.GetFromJsonAsync<ZipPageDto>("/api/admin/zipcodes?pageSize=1");
//        var cityId = cities!.Items.First().Id;

//        var createPayload = new UpsertHouseDto("Testhus", "Hyggeligt", "Strandvej 1", cityId, "Beskrivelse");

//        var createResponse = await _client.PostAsJsonAsync("/api/admin/houses", createPayload);
//        await createResponse.DumpIfError(_out);
//        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
//        var houseId = await createResponse.Content.ReadFromJsonAsync<Guid>();

//        var detail = await _client.GetFromJsonAsync<HouseDetailsDto>($"/api/admin/houses/{houseId}");
//        detail.Should().NotBeNull();
//        detail!.Title.Should().Be("Testhus");
//        detail.Address.Should().Be("Strandvej 1");

//        var updatePayload = new UpdateHouseDto("Opdateret Hus", "Ny", "Strandvej 2", cityId, "Ny beskrivelse", "Nye faciliteter");
//        var updateResponse = await _client.PutAsJsonAsync($"/api/admin/houses/{houseId}", updatePayload);
//        await updateResponse.DumpIfError(_out);
//        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

//        var updated = await _client.GetFromJsonAsync<HouseDetailsDto>($"/api/admin/houses/{houseId}");
//        updated!.Title.Should().Be("Opdateret Hus");
//        updated.Address.Should().Be("Strandvej 2");

//        var features = await _client.GetFromJsonAsync<List<FeatureDetailsDto>>("/api/admin/features");
//        features.Should().NotBeNull();

//        var featurePayload = new[]
//        {
//            new PostFeatureValueDto(features!.First().Id, "42")
//        };

//        var setFeatures = await _client.PostAsJsonAsync($"/api/admin/houses/{houseId}/features", featurePayload);
//        await setFeatures.DumpIfError(_out);
//        setFeatures.StatusCode.Should().Be(HttpStatusCode.NoContent);

//        var delete = await _client.DeleteAsync($"/api/admin/houses/{houseId}");
//        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

//        var afterDelete = await _client.GetAsync($"/api/admin/houses/{houseId}");
//        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
//    }

//    [Fact]
//    public async Task Create_InvalidCity_ReturnsBadRequest()
//    {
//        var payload = new UpsertHouseDto("Fejl", null, null, Guid.NewGuid(), null, null);
//        var res = await _client.PostAsJsonAsync("/api/admin/houses", payload);
//        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
//    }
//}
