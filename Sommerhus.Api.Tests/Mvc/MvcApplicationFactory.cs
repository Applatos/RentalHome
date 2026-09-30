using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Sommerhus.Api.Tests.Infrastructure;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// Hosts Sommerhus.Mvc in-process, with every API client talking to an in-process API.
/// </summary>
/// <remarks>
/// Sommerhus.Mvc uses top-level statements, so its <c>Program</c> is internal (and the name
/// <c>Program</c> here means Sommerhus.Api's). Any public type of the MVC assembly identifies
/// the entry point; <see cref="Sommerhus.Mvc.SharedResource"/> is used.
/// </remarks>
public sealed class MvcApplicationFactory(CustomWebApplicationFactory api)
    : WebApplicationFactory<Sommerhus.Mvc.SharedResource>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Read by Program.cs before the host is built, so it has to be a host setting.
        builder.UseSetting("Api:BaseUrl", "http://localhost/");

        builder.ConfigureServices(services =>
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(handlers =>
                    handlers.PrimaryHandler = api.Server.CreateHandler())));
    }

    /// <summary>A browser-like client: keeps cookies, does not follow redirects.</summary>
    public HttpClient CreateBrowser()
        => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
}

/// <summary>An MVC host with its API, plus a guest and an owner account. Admin is seeded.</summary>
public sealed class MvcAppFixture : IDisposable
{
    public const string AdminName = "admin";
    public const string AdminPassword = "Sommerhus123!";
    public const string UserName = "mvc-guest";
    public const string UserPassword = "MvcGuest123!";
    public const string OwnerName = "mvc-owner";
    public const string OwnerPassword = "MvcOwner123!";

    public MvcAppFixture()
    {
        Api = new CustomWebApplicationFactory();
        Mvc = new MvcApplicationFactory(Api);
        Api.CreateUserClient(UserName, UserPassword).Dispose();
        Api.CreateOwnerClient(OwnerName, OwnerPassword).Dispose();
    }

    public CustomWebApplicationFactory Api { get; }

    public MvcApplicationFactory Mvc { get; }

    public static string PasswordFor(string username) => username switch
    {
        AdminName => AdminPassword,
        UserName => UserPassword,
        OwnerName => OwnerPassword,
        _ => throw new ArgumentOutOfRangeException(nameof(username), username, null)
    };

    /// <summary>Posts the MVC login form and returns the response (a redirect on success).</summary>
    public static async Task<HttpResponseMessage> PostLoginAsync(
        HttpClient browser, string username, string? returnUrl = null)
    {
        var token = await GetAntiforgeryTokenAsync(browser, "/account/login");
        var fields = new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = PasswordFor(username),
            ["__RequestVerificationToken"] = token
        };
        if (returnUrl is not null)
            fields["ReturnUrl"] = returnUrl;

        return await browser.PostAsync("/account/login", new FormUrlEncodedContent(fields));
    }

    /// <summary>A client signed in as <paramref name="username"/> through the real login form.</summary>
    public async Task<HttpClient> SignInAsync(string username)
    {
        var browser = Mvc.CreateBrowser();
        using var response = await PostLoginAsync(browser, username);
        if (response.StatusCode != HttpStatusCode.Redirect)
            throw new InvalidOperationException($"Login as {username} failed with {(int)response.StatusCode}.");
        return browser;
    }

    /// <summary>
    /// A redirect's target within the test host as path and query (the cookie handler redirects
    /// to absolute URLs, MVC's own redirects are relative). A target elsewhere stays absolute.
    /// </summary>
    public static string LocationOf(HttpResponseMessage response)
    {
        var location = response.Headers.Location
            ?? throw new InvalidOperationException($"{(int)response.StatusCode} response has no Location.");
        return location.IsAbsoluteUri && location.Host == "localhost"
            ? location.PathAndQuery
            : location.OriginalString;
    }

    /// <summary>Reads the anti-forgery token from a page that renders a post form.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(HttpClient browser, string path)
    {
        using var response = await browser.GetAsync(path);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException($"GET {path} returned {(int)response.StatusCode}.");

        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        return match.Success
            ? match.Groups[1].Value
            : throw new InvalidOperationException($"GET {path} rendered no anti-forgery token.");
    }

    public void Dispose()
    {
        Mvc.Dispose();
        Api.Dispose();
    }
}
