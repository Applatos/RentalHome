using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Core;
using Sommerhus.Core.Data;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Infrastructure;

/// <summary>
/// Descriptions are stored as plain text. HTML stored by earlier releases is converted once,
/// when the application starts.
/// </summary>
public sealed class DescriptionTextTests : IDisposable
{
    private const string AdminUsername = "description-admin";
    private const string AdminPassword = "DescriptionTests123!";

    private readonly string directory = Path.Combine(
        Path.GetTempPath(), "sommerhus_description_tests", Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("<p>MAX 1 HUND.<br>Super dejligt hus.</p>\n<p>Næste &amp; sidste</p>", "MAX 1 HUND.\nSuper dejligt hus.\n\nNæste & sidste")]
    [InlineData("<p>One</p><p>Two</p>", "One\n\nTwo")]
    [InlineData("<div><strong>Pool</strong> og <em>spa</em></div>", "Pool og spa")]
    [InlineData("Linje<br/>Linje<BR>Linje", "Linje\nLinje\nLinje")]
    public void ToText_KeepsParagraphsAndLineBreaks(string html, string expected)
    {
        StoredHtmlDescriptions.LooksLikeHtml(html).Should().BeTrue();
        StoredHtmlDescriptions.ToText(html).Should().Be(expected);
    }

    [Theory]
    [InlineData("Plain text.\n\nSecond paragraph.")]
    [InlineData("Afstand < 100 m til stranden")]
    public void LooksLikeHtml_IgnoresPlainText(string text)
        => StoredHtmlDescriptions.LooksLikeHtml(text).Should().BeFalse();

    [Fact]
    public async Task Startup_ConvertsStoredHtml_AndReindexesTheHouse()
    {
        Guid houseId;
        using (var factory = CreateFactory())
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var city = await db.Cities.SingleAsync(c => c.Zip == "6857");
            var house = new VacationHouse
            {
                Title = "Html house",
                CityId = city.Id,
                Status = EntityStatus.Published,
                PublishedAtUtc = DateTime.UtcNow,
                Description = "<p>Første afsnit.<br>Anden linje.</p>\n<p>Andet afsnit &amp; mere.</p>",
                Images = [new HouseImage { FileName = "cover.jpg", Kind = ImageKind.Cover }]
            };
            db.Houses.Add(house);
            db.Areas.Add(new Area { Name = "Html area", Description = "<p>Et område.</p>" });
            await db.SaveChangesAsync();
            houseId = house.Id;
        }

        using (var factory = CreateFactory())
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            (await db.Houses.SingleAsync(h => h.Id == houseId)).Description
                .Should().Be("Første afsnit.\nAnden linje.\n\nAndet afsnit & mere.");
            (await db.Areas.SingleAsync(a => a.Name == "Html area")).Description.Should().Be("Et område.");

            var page = await client.GetFromJsonAsync<PageResult<PublicHouseListItemDto>>("/api/houses");
            var item = page!.Items.Should().ContainSingle(h => h.Id == houseId).Which;
            item.Description.Should().NotContain("<");
            item.Summary.Should().Be("Første afsnit. Anden linje. Andet afsnit & mere.");
        }
    }

    private ProductionFactory CreateFactory() => new(directory, AdminUsername, AdminPassword);

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
