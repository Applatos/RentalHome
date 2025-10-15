using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Contracts.Dtos._api.Areas;
using Sommerhus.Contracts.Dtos.Shared;
using Xunit.Abstractions;

namespace Sommerhus.Api.Tests.Admin;

public class AreasTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _out;

    public AreasTests(CustomWebApplicationFactory f, ITestOutputHelper o)
    {
        _client = f.CreateClient();
        _out = o;
    }

    [Fact]
    public async Task List_ReturnsSeeded()
    {
        var res = await _client.GetAsync("/api/admin/areas");
        await res.DumpIfError(_out);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await res.Content.ReadFromJsonAsync<List<AreaListItemDto>>();
        data.Should().NotBeNull();
        data!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Create_Read_Update_Delete_Flow_Works()
    {
        var createDto = new UpsertAreaDto("Test Area", null, "Desc", new List<string> { "a.jpg", "b.jpg" });
        var createRes = await _client.PostAsJsonAsync("/api/admin/areas", createDto);
        var created = await createRes.ReadJsonOrDump<AreaDetailsDto>(_out);

        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Test Area");
        created.Images.Should().HaveCount(2);
        created.Slug.Should().NotBeNullOrWhiteSpace();
        created.CityId.Should().BeNull();

        var fetched = await _client.GetFromJsonAsync<AreaDetailsDto>($"/api/admin/areas/{created.Id}");
        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be("Test Area");
        fetched.Images.Should().HaveCount(2);

        var updateDto = new UpsertHouseDto("Updated Area", null, "New Desc", new List<string> { "x.png" });
        var updateRes = await _client.PutAsJsonAsync($"/api/admin/areas/{created.Id}", updateDto);
        await updateRes.DumpIfError(_out);
        updateRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterUpdate = await _client.GetFromJsonAsync<AreaDetailsDto>($"/api/admin/areas/{created.Id}");
        afterUpdate.Should().NotBeNull();
        afterUpdate!.Name.Should().Be("Updated Area");
        afterUpdate.Description.Should().Be("New Desc");
        afterUpdate.Images.Should().HaveCount(1);
        afterUpdate.Slug.Should().NotBe(created.Slug);
        afterUpdate.CityId.Should().BeNull();

        var deleteRes = await _client.DeleteAsync($"/api/admin/areas/{created.Id}");
        await deleteRes.DumpIfError(_out);
        deleteRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missing = await _client.GetAsync($"/api/admin/areas/{created.Id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
