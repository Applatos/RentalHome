//using System.Net;
//using System.Net.Http.Json;
//using FluentAssertions;
//using Sommerhus.Contracts.Dtos._api.Cities;
//using Sommerhus.Api.Tests.Infrastructure;
//using Xunit.Abstractions;

//namespace Sommerhus.Api.Tests.Admin;

//public class ZipCodesTests : IClassFixture<CustomWebApplicationFactory>
//{
//    private readonly HttpClient _client;
//    private readonly ITestOutputHelper _out;

//    public ZipCodesTests(CustomWebApplicationFactory f, ITestOutputHelper o)
//    {
//        _client = f.CreateClient();
//        _out = o;
//    }

//    [Fact]
//    public async Task Crud_Works()
//    {
//        var create = new CreateZipDto("9999", "Testby");
//        var cRes = await _client.PostAsJsonAsync("/api/admin/zipcodes", create);
//        var created = await cRes.ReadJsonOrDump<ZipDto>(_out);

//        cRes.StatusCode.Should().Be(HttpStatusCode.Created);
//        created!.Zip.Should().Be("9999");

//        var get = await _client.GetFromJsonAsync<ZipDto>($"/api/admin/zipcodes/{created.Id}");
//        get!.City.Should().Be("Testby");

//        var upd = await _client.PutAsJsonAsync($"/api/admin/zipcodes/{created.Id}", new CreateZipDto("8888", "Nyby"));
//        await upd.DumpIfError(_out);
//        upd.StatusCode.Should().Be(HttpStatusCode.NoContent);

//        var get2 = await _client.GetFromJsonAsync<ZipDto>($"/api/admin/zipcodes/{created.Id}");
//        get2!.Zip.Should().Be("8888");

//        var del = await _client.DeleteAsync($"/api/admin/zipcodes/{created.Id}");
//        await del.DumpIfError(_out);
//        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
//    }
//}
