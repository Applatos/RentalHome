using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Contracts.Dtos.Admin.Areas;
using Sommerhus.Api.Tests.Infrastructure;
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
        data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Create_Get_Update_Delete_Works()
    {
        // Create
        var create = new CreateAreaDto("Test Area", "Desc", null, new List<string> { "a.jpg", "b.jpg" });
        var createRes = await _client.PostAsJsonAsync("/api/admin/areas", create);
        var created = await createRes.ReadJsonOrDump<AreaDetailDto>(_out);

        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.Name.Should().Be("Test Area");
        created.Images.Should().HaveCount(2);

        // Get
        var get = await _client.GetFromJsonAsync<AreaDetailDto>($"/api/admin/areas/{created.Id}");
        get!.Name.Should().Be("Test Area");

        // Update (replace images)
        var update = new UpdateAreaDto
        {
            Name = "Updated Area",
            Description = "New Desc",
            CityId = null,
            Images = new List<string> { "x.png" }
        };
        var updRes = await _client.PutAsJsonAsync($"/api/admin/areas/{created.Id}", update);
        await updRes.DumpIfError(_out);
        updRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var after = await _client.GetFromJsonAsync<AreaDetailDto>($"/api/admin/areas/{created.Id}");
        after!.Name.Should().Be("Updated Area");
        //after.Images.Should().HaveCount(1);

        // Delete
        var del = await _client.DeleteAsync($"/api/admin/areas/{created.Id}");
        await del.DumpIfError(_out);
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var missing = await _client.GetAsync($"/api/admin/areas/{created.Id}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
