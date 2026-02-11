using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Dtos.Auth;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Api.Tests.Auth;

public sealed class MultiRoleAuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;

    public MultiRoleAuthTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsTokenWithUserRole()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "newuser1",
            Email = "newuser1@test.local",
            Password = "NewUser123!",
            FirstName = "New",
            LastName = "User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        token.Should().NotBeNull();
        token!.Role.Should().Be("User");
        token.Username.Should().Be("newuser1");
        token.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsValidationError()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "dupuser",
            Email = "dup1@test.local",
            Password = "DupUser123!"
        });

        var response = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "dupuser",
            Email = "dup2@test.local",
            Password = "DupUser123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidUser_ReturnsToken()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "loginuser",
            Email = "loginuser@test.local",
            Password = "LoginUser123!"
        });

        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Username = "loginuser",
            Password = "LoginUser123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<AuthTokenResponse>();
        token.Should().NotBeNull();
        token!.Role.Should().Be("User");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "wrongpwuser",
            Email = "wrongpw@test.local",
            Password = "WrongPw123!"
        });

        var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Username = "wrongpwuser",
            Password = "BadPassword999!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoint_UserRole_ReturnsForbidden()
    {
        using var userClient = factory.CreateUserClient("forbiduser");

        var response = await userClient.GetAsync("api/admin/houses");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/admin/houses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_AuthenticatedUser_CanGetAndUpdate()
    {
        using var userClient = factory.CreateUserClient("profileuser");

        var getResponse = await userClient.GetAsync("api/me/profile");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Username.Should().Be("profileuser");
        profile.Role.Should().Be("User");
        profile.FirstName.Should().Be("Test");

        var updateResponse = await userClient.PutAsJsonAsync("api/me/profile", new UpdateProfileRequest
        {
            FirstName = "Updated",
            LastName = "Name",
            Phone = "12345678"
        });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse2 = await userClient.GetAsync("api/me/profile");
        var updated = await getResponse2.Content.ReadFromJsonAsync<UserProfileDto>();
        updated!.FirstName.Should().Be("Updated");
        updated.Phone.Should().Be("12345678");
    }

    [Fact]
    public async Task Profile_Anonymous_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminUsers_ListUsers_ReturnsAllUsers()
    {
        using var adminClient = factory.CreateAuthenticatedClient();

        var response = await adminClient.GetAsync("api/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserListItemDto>>();
        users.Should().NotBeNull();
        users!.Count.Should().BeGreaterThanOrEqualTo(1);
        users.Should().Contain(u => u.Role == "Admin");
    }

    [Fact]
    public async Task AdminUsers_ChangeRole_UpdatesUserRole()
    {
        // Register a user
        var client = factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = "rolechangeuser",
            Email = "rolechange@test.local",
            Password = "RoleChange123!"
        });
        regResponse.EnsureSuccessStatusCode();
        var token = await regResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token!.Token);
        var userId = jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier
            || c.Type == "sub").Value;

        // Admin changes role to HouseOwner
        using var adminClient = factory.CreateAuthenticatedClient();
        var changeResponse = await adminClient.PutAsJsonAsync($"api/admin/users/{userId}/role",
            new ChangeUserRoleRequest { Role = "HouseOwner" });
        changeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Re-login and verify role
        var loginResponse = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Username = "rolechangeuser",
            Password = "RoleChange123!"
        });
        var newToken = await loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        newToken!.Role.Should().Be("HouseOwner");
    }

    [Fact]
    public async Task OwnerEndpoint_UserRole_ReturnsForbidden()
    {
        using var userClient = factory.CreateUserClient("ownerendpointuser");

        var response = await userClient.GetAsync("api/owner/houses");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task OwnerEndpoint_OwnerRole_ReturnsOk()
    {
        using var ownerClient = factory.CreateOwnerClient("ownerlistuser");

        var response = await ownerClient.GetAsync("api/owner/houses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
