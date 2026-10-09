using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Core;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// The public pages against the development seed: area Blåvand with the house "Blåvand Strand 4".
/// </summary>
public sealed class PublicPagesTests(MvcAppFixture app) : IClassFixture<MvcAppFixture>
{
    [Fact]
    public async Task HouseList_ShowsTheHouseAsACard()
    {
        using var browser = app.Mvc.CreateBrowser();

        var html = await browser.GetStringAsync("/houses?culture=da-DK");

        html.Should().Contain("<title>Huse · Sommerhuse</title>")
            .And.Contain("class=\"house-card\"")
            .And.Contain("<span class=\"media-chip media-chip--area\">Blåvand</span>")
            .And.Contain("Blåvand Strand 4")
            .And.Contain("Se huset");
    }

    [Fact]
    public async Task AreaPage_ShowsItsHousesAsCards()
    {
        using var browser = app.Mvc.CreateBrowser();

        var html = await browser.GetStringAsync($"/areas/{await SeededAreaIdAsync()}?culture=da-DK");

        html.Should().Contain("<title>Blåvand · Sommerhuse</title>")
            .And.Contain("Sommerhuse i Blåvand")
            .And.Contain("class=\"house-card\"")
            .And.Contain("Blåvand Strand 4");
    }

    [Fact]
    public async Task HousePage_RendersTheDescriptionAsEncodedParagraphs()
    {
        using var browser = app.Mvc.CreateBrowser();
        using (var scope = app.Api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var house = await db.Houses.SingleAsync(h => h.Title == "Blåvand Strand 4");
            house.Description = "Første afsnit <b>fed</b>.\nAnden linje.\n\nAndet afsnit.";
            await db.SaveChangesAsync();
        }

        using var response = await browser.GetAsync($"/houses/{await SeededHouseIdAsync()}?culture=da-DK");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("<p>Første afsnit &lt;b&gt;fed&lt;/b&gt;.<br>Anden linje.</p><p>Andet afsnit.</p>");
    }

    private async Task<Guid> SeededAreaIdAsync()
    {
        using var scope = app.Api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Areas.Where(a => a.Name == "Blåvand").Select(a => a.Id).SingleAsync();
    }

    private async Task<Guid> SeededHouseIdAsync()
    {
        using var scope = app.Api.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Houses.Where(h => h.Title == "Blåvand Strand 4").Select(h => h.Id).SingleAsync();
    }
}
