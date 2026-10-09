using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Admin;

/// <summary>
/// The demo content import against a real Production startup, with the content folder the API
/// ships (copied beside the test binaries).
/// </summary>
public sealed class DemoContentImportTests : IDisposable
{
    private const string AdminUsername = "demo-admin";
    private const string AdminPassword = "DemoContentTests123!";
    private const string Blaavand = "Blåvand Strand 4";
    private const string Almosetoften = "Almosetoften 10";

    private readonly string directory = Path.Combine(
        Path.GetTempPath(), "sommerhus_demo_content_tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Import_OnFreshProductionDatabase_CreatesPublishedHousesAreasAndFeatures()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateAdminClient();

        var result = await ImportAsync(client);

        result.Succeeded.Should().BeTrue(result.Error ?? string.Empty);
        result.Notes.Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var houses = await db.Houses
            .Include(h => h.Images)
            .Include(h => h.HouseFeatures)
            .Include(h => h.Areas)
            .OrderBy(h => h.Title)
            .ToListAsync();
        houses.Select(h => (h.Title, h.Status)).Should().Equal(
            (Almosetoften, EntityStatus.Published),
            (Blaavand, EntityStatus.Published));

        var blaavand = houses.Single(h => h.Title == Blaavand);
        blaavand.Areas.Select(a => a.Name).Should().Equal("Blåvand");
        blaavand.HouseFeatures.Should().HaveCount(18);
        // 36 photos and the floor plan; the two duplicate photos are skipped.
        blaavand.Images.Should().HaveCount(37);
        blaavand.Images.Should().ContainSingle(i => i.Kind == ImageKind.Cover);
        blaavand.Images.Should().ContainSingle(i => i.Kind == ImageKind.Floorplan);
        Directory.GetFiles(Path.Combine(factory.WebRoot, "uploads", "houses", blaavand.Id.ToString()))
            .Should().HaveCount(37);
        blaavand.Description.Should().StartWith("<p>MAX 1 HUND - INGEN UNGDOMSGRUPPER.<br>Super dejligt poolhus")
            .And.NotContain("Beskrivelse")
            .And.NotContain("Gratis slutrengøring");

        var almosetoften = houses.Single(h => h.Title == Almosetoften);
        almosetoften.Areas.Select(a => a.Name).Should().Equal("Ho");
        almosetoften.Images.Should().HaveCount(31);

        var summaries = await db.HousePriceSummaries.ToDictionaryAsync(s => s.HouseId);
        (summaries[blaavand.Id].MinNightlyPrice, summaries[blaavand.Id].MaxNightlyPrice).Should().Be((1700m, 3800m));
        (summaries[almosetoften.Id].MinNightlyPrice, summaries[almosetoften.Id].MaxNightlyPrice).Should().Be((1500m, 3400m));

        var areas = await db.Areas.Include(a => a.AreaImages).OrderBy(a => a.Name).ToListAsync();
        areas.Select(a => (a.Name, a.Status, a.AreaImages.Count)).Should().Equal(
            ("Blåvand", EntityStatus.Published, 4),
            ("Ho", EntityStatus.Published, 0));
        areas[0].Description.Should().StartWith("<p>Blåvand er en mindre turist- og badebebyggelse");

        var features = await db.Features.ToListAsync();
        features.Should().HaveCount(18);
        // Every icon but the cleaning one, which has no feature.
        features.Count(f => f.IconUrl != null).Should().Be(10);
        features.Single(f => f.Key == "wifi").Name.Should().Be("Trådløst internet");

        var group = await db.HouseGroups.SingleAsync();
        group.Name.Should().Be("Vesterhavet");
        houses.Should().OnlyContain(h => h.GroupId == group.Id);
        var year = DateTime.UtcNow.Year;
        (await db.SeasonSpans.Where(s => s.CalendarId == group.DefaultCalendarId).MaxAsync(s => s.EndDate))
            .Should().Be(new DateOnly(year + 2, 12, 31));

        using var anonymous = factory.CreateClient();
        var search = await anonymous.GetFromJsonAsync<JsonElement>("/api/houses");
        search.GetProperty("total").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task Import_RunTwice_SecondRunChangesNothing()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateAdminClient();

        (await ImportAsync(client)).Succeeded.Should().BeTrue();
        var before = await SnapshotAsync(factory);

        var second = await ImportAsync(client);

        second.Succeeded.Should().BeTrue(second.Error ?? string.Empty);
        second.Changes.Should().BeEmpty();
        (await SnapshotAsync(factory)).Should().Equal(before);
    }

    [Fact]
    public async Task Import_ExistingFeatureWithSameKey_IsUpdatedNotDuplicated()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateAdminClient();
        var handMade = new Feature { Name = "sauna", Key = "sauna", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Other };
        await WithDbAsync(factory, async db =>
        {
            db.Features.Add(handMade);
            await db.SaveChangesAsync();
        });

        var result = await ImportAsync(client);

        result.Succeeded.Should().BeTrue(result.Error ?? string.Empty);
        result.Changes.Should().Contain(c => c.StartsWith("Features: updated") && c.Contains("sauna"));
        await WithDbAsync(factory, async db =>
        {
            var sauna = await db.Features.SingleAsync(f => f.Key == "sauna");
            sauna.Id.Should().Be(handMade.Id);
            sauna.Name.Should().Be("Sauna");
            sauna.Category.Should().Be(FeatureCategory.Facility);
            sauna.IconUrl.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Import_HouseWithSameTitleExists_LeavesItAndCreatesTheOther()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateAdminClient();
        await WithDbAsync(factory, async db =>
        {
            var city = await db.Cities.SingleAsync(c => c.Zip == "6857");
            db.Houses.Add(new VacationHouse { Title = Almosetoften, CityId = city.Id, Description = "Made by hand." });
            await db.SaveChangesAsync();
        });

        var result = await ImportAsync(client);

        result.Succeeded.Should().BeTrue(result.Error ?? string.Empty);
        result.Notes.Should().ContainSingle(n => n.Contains(Almosetoften));
        await WithDbAsync(factory, async db =>
        {
            var existing = await db.Houses.Include(h => h.Images).SingleAsync(h => h.Title == Almosetoften);
            existing.Description.Should().Be("Made by hand.");
            existing.Status.Should().Be(EntityStatus.Draft);
            existing.Images.Should().BeEmpty();
            (await db.Houses.CountAsync(h => h.Title == Blaavand)).Should().Be(1);
        });
    }

    [Fact]
    public async Task Import_AfterAdminEdits_KeepsTheEditsAndAnUnpublishedHouseUnpublished()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateAdminClient();
        (await ImportAsync(client)).Succeeded.Should().BeTrue();
        await WithDbAsync(factory, async db =>
        {
            var house = await db.Houses.SingleAsync(h => h.Title == Blaavand);
            house.Title = "Blåvand Strand 4 (renamed)";
            house.Description = "Edited by an admin.";
            await db.SaveChangesAsync();
        });
        var houseId = await WithDbAsync(factory, db => db.Houses.Where(h => h.Title == Almosetoften).Select(h => h.Id).SingleAsync());
        using (var unpublish = await client.PostAsJsonAsync($"/api/admin/houses/{houseId}/status", new ChangeStatusDto { Target = EntityStatus.Draft }))
            unpublish.EnsureSuccessStatusCode();

        var result = await ImportAsync(client);

        result.Succeeded.Should().BeTrue(result.Error ?? string.Empty);
        result.Changes.Should().BeEmpty();
        await WithDbAsync(factory, async db =>
        {
            var houses = await db.Houses.OrderBy(h => h.Title).ToListAsync();
            houses.Select(h => (h.Title, h.Status)).Should().Equal(
                (Almosetoften, EntityStatus.Draft),
                ("Blåvand Strand 4 (renamed)", EntityStatus.Published));
            houses[1].Description.Should().Be("Edited by an admin.");
        });
    }

    [Fact]
    public async Task Import_WithoutAdminToken_IsRejected()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/admin/demo-content/import", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private ProductionFactory CreateFactory() => new(directory, AdminUsername, AdminPassword);

    private static async Task<DemoContentImportResultDto> ImportAsync(HttpClient client)
    {
        using var response = await client.PostAsync("/api/admin/demo-content/import", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<DemoContentImportResultDto>())!;
    }

    private static async Task<List<string>> SnapshotAsync(ProductionFactory factory)
    {
        var rows = await WithDbAsync(factory, async db => new List<string>
        {
            $"houses {await db.Houses.CountAsync()}",
            $"areas {await db.Areas.CountAsync()}",
            $"features {await db.Features.CountAsync()}",
            $"house images {await db.Images.CountAsync()}",
            $"area images {await db.AreaImages.CountAsync()}",
            $"house features {await db.HouseFeatures.CountAsync()}",
            $"price plans {await db.PricePlans.CountAsync()}",
            $"season prices {await db.SeasonPrices.CountAsync()}",
            $"season spans {await db.SeasonSpans.CountAsync()}",
            $"season codes {await db.SeasonCodes.CountAsync()}",
            $"groups {await db.HouseGroups.CountAsync()}",
        });
        rows.Add($"files {Directory.GetFiles(Path.Combine(factory.WebRoot, "uploads"), "*", SearchOption.AllDirectories).Length}");
        return rows;
    }

    private static async Task<T> WithDbAsync<T>(ProductionFactory factory, Func<AppDbContext, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private static Task WithDbAsync(ProductionFactory factory, Func<AppDbContext, Task> action)
        => WithDbAsync(factory, async db =>
        {
            await action(db);
            return true;
        });

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
