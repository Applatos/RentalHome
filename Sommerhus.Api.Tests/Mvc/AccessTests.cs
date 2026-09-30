using System.Net;
using FluentAssertions;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>Where login, logout and missing access take people in the MVC site.</summary>
public sealed class AccessTests(MvcAppFixture app) : IClassFixture<MvcAppFixture>
{
    [Theory]
    [InlineData(MvcAppFixture.UserName)]
    [InlineData(MvcAppFixture.OwnerName)]
    public async Task Login_AsGuestOrOwner_LandsOnPublicHouseList(string username)
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await MvcAppFixture.PostLoginAsync(browser, username);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be("/houses");
    }

    [Fact]
    public async Task Login_AsAdmin_LandsInAdmin()
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await MvcAppFixture.PostLoginAsync(browser, MvcAppFixture.AdminName);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be("/admin/houses");
    }

    [Theory]
    [InlineData("/bookings", "/bookings")]
    [InlineData("https://evil.example/", "/houses")]
    [InlineData("//evil.example/", "/houses")]
    public async Task Login_HonoursOnlyLocalReturnUrl(string returnUrl, string expected)
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await MvcAppFixture.PostLoginAsync(browser, MvcAppFixture.UserName, returnUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be(expected);
    }

    [Theory]
    [InlineData(MvcAppFixture.UserName)]
    [InlineData(MvcAppFixture.OwnerName)]
    public async Task NonAdmin_RequestingAdmin_IsSentToAccessDenied_NotToLogin(string username)
    {
        using var browser = await app.SignInAsync(username);

        using var response = await browser.GetAsync("/admin/houses");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = MvcAppFixture.LocationOf(response);
        location.Should().StartWith("/account/access-denied");
        location.Should().NotContain("/account/login");

        using var page = await browser.GetAsync(location);
        page.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await page.Content.ReadAsStringAsync();
        html.Should().Contain("Ingen adgang");
        html.Should().Contain("action=\"/account/switch-user\"");
    }

    [Fact]
    public async Task NonAdmin_FollowingRedirectsFromAdmin_EndsOnAPageWithoutLooping()
    {
        using var browser = await app.SignInAsync(MvcAppFixture.UserName);
        var path = "/admin/houses";
        var hops = new List<string> { path };

        HttpResponseMessage response;
        while (true)
        {
            response = await browser.GetAsync(path);
            if (response.StatusCode != HttpStatusCode.Redirect)
                break;
            path = MvcAppFixture.LocationOf(response);
            hops.Add(path);
            response.Dispose();
            hops.Should().HaveCountLessThan(5, "the redirects were {0}", string.Join(" -> ", hops));
        }

        using (response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            path.Should().StartWith("/account/access-denied");
        }
    }

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    public async Task SignedInGuest_OpeningLoginOrRegister_GoesToTheirLandingPage(string path)
    {
        using var browser = await app.SignInAsync(MvcAppFixture.UserName);

        using var response = await browser.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be("/houses");
    }

    [Theory]
    [InlineData("/account/login?returnUrl=%2Fbookings%3Fx%3D1", "/bookings?x=1")]
    [InlineData("/account/register?returnUrl=%2Fbookings%3Fx%3D1", "/bookings?x=1")]
    [InlineData("/account/login?returnUrl=https%3A%2F%2Fevil.example%2F", "/houses")]
    public async Task SignedInGuest_OpeningLoginOrRegister_HonoursOnlyALocalReturnUrl(string path, string expected)
    {
        // A page rendered before signing in can link here with the visitor's booking choice.
        using var browser = await app.SignInAsync(MvcAppFixture.UserName);

        using var response = await browser.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be(expected);
    }

    [Fact]
    public async Task AccessDenied_IsOpenToAnonymousVisitors()
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await browser.GetAsync("/account/access-denied");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<title>Ingen adgang</title>");
        html.Should().Contain("Se sommerhusene");
        // Nobody to sign out: the page offers a plain login instead.
        html.Should().NotContain("switch-user");
    }

    [Fact]
    public async Task AccessDenied_IsLocalized()
    {
        using var browser = app.Mvc.CreateBrowser();

        var html = await browser.GetStringAsync("/account/access-denied?culture=en-GB");

        html.Should().Contain("<title>Access denied</title>");
        html.Should().Contain("You do not have access to the page you tried to open.");
    }

    [Fact]
    public async Task AnonymousVisitor_RequestingAdmin_IsSentToLogin()
    {
        using var browser = app.Mvc.CreateBrowser();

        using var response = await browser.GetAsync("/admin/houses");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().StartWith("/account/login?ReturnUrl=");
    }

    [Fact]
    public async Task SwitchUser_SignsOutAndGoesToLogin_KeepingTheReturnUrl()
    {
        using var browser = await app.SignInAsync(MvcAppFixture.UserName);
        var token = await MvcAppFixture.GetAntiforgeryTokenAsync(browser, "/account/access-denied");

        using var response = await browser.PostAsync("/account/switch-user", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["returnUrl"] = "/admin/houses",
                ["__RequestVerificationToken"] = token
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be("/account/login?returnUrl=%2Fadmin%2Fhouses");

        // Signed out: the login form is shown instead of a redirect.
        using var login = await browser.GetAsync(MvcAppFixture.LocationOf(response));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_LandsOnPublicHouseList()
    {
        using var browser = await app.SignInAsync(MvcAppFixture.OwnerName);
        var token = await MvcAppFixture.GetAntiforgeryTokenAsync(browser, "/account/access-denied");

        using var response = await browser.PostAsync("/account/logout", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        MvcAppFixture.LocationOf(response).Should().Be("/houses");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(MvcAppFixture.UserName, false)]
    [InlineData(MvcAppFixture.OwnerName, false)]
    [InlineData(MvcAppFixture.AdminName, true)]
    public async Task AdminNavLink_IsShownOnlyToAdmins(string? username, bool shown)
    {
        using var browser = username is null ? app.Mvc.CreateBrowser() : await app.SignInAsync(username);

        var html = await browser.GetStringAsync("/account/access-denied");

        if (shown)
            html.Should().Contain("<a class=\"nav-link\" href=\"/admin/houses\">Admin</a>");
        else
            html.Should().NotContain("href=\"/admin/houses\"");
    }
}
