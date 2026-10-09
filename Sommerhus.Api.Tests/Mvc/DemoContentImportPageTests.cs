using System.Net;
using FluentAssertions;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// The admin button that runs the demo content import. The API behind it has the development
/// seed, whose "Blåvand Strand 4" the import must leave alone.
/// </summary>
public sealed class DemoContentImportPageTests(MvcAppFixture app) : IClassFixture<MvcAppFixture>
{
    [Fact]
    public async Task ImportButton_ReportsTheImport_AndASecondPressHasNothingToDo()
    {
        using var browser = await app.SignInAsync(MvcAppFixture.AdminName);

        (await browser.GetStringAsync("/admin/houses")).Should().Contain("action=\"/admin/demo-content/import\"");

        using (var first = await PressImportAsync(browser))
            MvcAppFixture.LocationOf(first).Should().Be("/admin/houses");
        var afterFirst = await browser.GetStringAsync("/admin/houses");
        afterFirst.Should().Contain("Demo-indhold importeret:")
            .And.Contain("Almosetoften 10")
            .And.Contain("already exists and was left as it is");

        using (await PressImportAsync(browser)) { }
        (await browser.GetStringAsync("/admin/houses")).Should().Contain("Demo-indholdet er allerede importeret");
    }

    private static async Task<HttpResponseMessage> PressImportAsync(HttpClient browser)
    {
        var token = await MvcAppFixture.GetAntiforgeryTokenAsync(browser, "/admin/houses");
        var response = await browser.PostAsync("/admin/demo-content/import", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["__RequestVerificationToken"] = token }));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        return response;
    }
}
