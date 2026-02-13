using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Api.Tests.Public;

public sealed class FavoriteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _adminClient;

    public FavoriteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _adminClient = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task AddFavorite_PublishedHouse_Succeeds()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser1");

        var res = await userClient.PostAsync($"api/me/favorites/{houseId}", null);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ReturnsSuccess()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser2");

        await userClient.PostAsync($"api/me/favorites/{houseId}", null);
        var res = await userClient.PostAsync($"api/me/favorites/{houseId}", null);
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AddFavorite_NonExistentHouse_ReturnsNotFound()
    {
        var userClient = _factory.CreateUserClient("favuser3");

        var res = await userClient.PostAsync($"api/me/favorites/{Guid.NewGuid()}", null);
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListFavorites_ReturnsAddedHouses()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser4");

        await userClient.PostAsync($"api/me/favorites/{houseId}", null);

        var res = await userClient.GetAsync("api/me/favorites");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var favorites = await res.Content.ReadFromJsonAsync<List<FavoriteHouseDto>>();
        favorites.Should().NotBeNull();
        favorites!.Should().Contain(f => f.HouseId == houseId);
    }

    [Fact]
    public async Task RemoveFavorite_ExistingFavorite_Succeeds()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser5");

        await userClient.PostAsync($"api/me/favorites/{houseId}", null);

        var res = await userClient.DeleteAsync($"api/me/favorites/{houseId}");
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listRes = await userClient.GetAsync("api/me/favorites");
        var favorites = await listRes.Content.ReadFromJsonAsync<List<FavoriteHouseDto>>();
        favorites!.Should().NotContain(f => f.HouseId == houseId);
    }

    [Fact]
    public async Task RemoveFavorite_NotFavorited_ReturnsSuccess()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser6");

        var res = await userClient.DeleteAsync($"api/me/favorites/{houseId}");
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ListFavorites_Anonymous_ReturnsUnauthorized()
    {
        var anonClient = _factory.CreateClient();

        var res = await anonClient.GetAsync("api/me/favorites");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HouseDetail_Authenticated_IncludesIsFavorite()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var userClient = _factory.CreateUserClient("favuser7");

        await userClient.PostAsync($"api/me/favorites/{houseId}", null);

        var res = await userClient.GetAsync($"api/houses/{houseId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var house = await res.Content.ReadFromJsonAsync<PublicHouseDetailsDto>();
        house.Should().NotBeNull();
        house!.IsFavorite.Should().BeTrue();
    }

    [Fact]
    public async Task HouseDetail_Anonymous_IsFavoriteIsNull()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var anonClient = _factory.CreateClient();

        var res = await anonClient.GetAsync($"api/houses/{houseId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var house = await res.Content.ReadFromJsonAsync<PublicHouseDetailsDto>();
        house.Should().NotBeNull();
        house!.IsFavorite.Should().BeNull();
    }

    [Fact]
    public async Task ListFavorites_DifferentUsers_Isolated()
    {
        var houseId = await GetPublishedHouseIdAsync();
        var user1 = _factory.CreateUserClient("favuser8a");
        var user2 = _factory.CreateUserClient("favuser8b");

        await user1.PostAsync($"api/me/favorites/{houseId}", null);

        var res = await user2.GetAsync("api/me/favorites");
        var favorites = await res.Content.ReadFromJsonAsync<List<FavoriteHouseDto>>();
        favorites!.Should().NotContain(f => f.HouseId == houseId);
    }

    private async Task<Guid> GetPublishedHouseIdAsync()
    {
        var houses = await _adminClient.GetFromJsonAsync<PageResult<AdminHouseListItemDto>>("api/admin/houses?pageSize=1");
        var houseId = houses!.Items.First().Id;
        _factory.EnsureHousePublished(houseId);
        return houseId;
    }
}
