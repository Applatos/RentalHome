using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Infrastructure;

public sealed class ProductionStartupTests : IDisposable
{
    private const string AdminUsername = "startup-admin";
    private const string AdminPassword = "StartupTests123!";
    private readonly string directory = Path.Combine(
        Path.GetTempPath(), "sommerhus_production_startup_tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task FreshProductionStartup_SeedsCitiesWithoutDemoData_AndAdminCanCreateHouse()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var cities = await client.GetFromJsonAsync<List<CityDto>>("/api/cities");
        cities.Should().NotBeNull();
        cities!.Count.Should().BeGreaterThan(1000);
        var city = cities.Should().ContainSingle(c => c.Zip == "6857").Which;
        city.Name.Should().Be("Blåvand");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Houses.AnyAsync()).Should().BeFalse();
            (await db.Features.AnyAsync()).Should().BeFalse();
            (await db.Areas.AnyAsync()).Should().BeFalse();
            (await db.Users.Select(u => u.UserName).ToListAsync())
                .Should().Equal(AdminUsername);
        }

        using var login = await client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest
        {
            Username = AdminUsername,
            Password = AdminPassword
        });
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AdminTokenResponse>();
        token.Should().NotBeNull();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);

        using var response = await client.PostAsJsonAsync("/api/admin/houses", new UpsertHouseDto
        {
            Title = "Production startup house",
            Address = "Strandvej 1",
            CityId = city.Id,
            Description = "A house created using the production city list."
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var houseId = await response.Content.ReadFromJsonAsync<Guid>();

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = await verificationDb.Houses.SingleAsync();
        house.Id.Should().Be(houseId);
        house.CityId.Should().Be(city.Id);
    }

    [Fact]
    public async Task ProductionRestarts_RepairMissingCityWithExistingFeatures_AndPreserveExistingData()
    {
        var customCity = new City
        {
            Name = "Custom destination",
            Zip = "TEST-0000",
            Description = "Locally maintained city."
        };
        var feature = new Feature
        {
            Name = "Existing production feature",
            Key = "production_feature",
            ValueType = FeatureValueType.Bool
        };
        Guid editedCityId;
        Dictionary<string, Guid> existingCityIds;

        using (var factory = CreateFactory())
        {
            using var client = factory.CreateClient();
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var editedCity = await db.Cities.SingleAsync(c => c.Zip == "6857");
            editedCityId = editedCity.Id;
            editedCity.Name = "Locally edited Blåvand";
            editedCity.Description = "Preserve this editorial description.";
            db.Cities.Remove(await db.Cities.SingleAsync(c => c.Zip == "8000"));
            db.Cities.Add(customCity);
            db.Features.Add(feature);
            await db.SaveChangesAsync();
            existingCityIds = await db.Cities.ToDictionaryAsync(c => c.Zip, c => c.Id);
        }

        Dictionary<string, Guid> repairedCityIds;
        using (var factory = CreateFactory())
        {
            using var client = factory.CreateClient();
            var cities = await client.GetFromJsonAsync<List<CityDto>>("/api/cities");
            cities.Should().NotBeNull();
            cities!.Should().ContainSingle(c => c.Zip == "8000");
            cities.Count.Should().Be(existingCityIds.Count + 1);

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var editedCity = await db.Cities.SingleAsync(c => c.Zip == "6857");
            editedCity.Id.Should().Be(editedCityId);
            editedCity.Name.Should().Be("Locally edited Blåvand");
            editedCity.Description.Should().Be("Preserve this editorial description.");
            var retainedCustomCity = await db.Cities.SingleAsync(c => c.Zip == customCity.Zip);
            retainedCustomCity.Id.Should().Be(customCity.Id);
            retainedCustomCity.Name.Should().Be(customCity.Name);
            retainedCustomCity.Description.Should().Be(customCity.Description);
            (await db.Features.Select(f => f.Id).ToListAsync()).Should().Equal(feature.Id);
            (await db.Houses.AnyAsync()).Should().BeFalse();
            repairedCityIds = await db.Cities.ToDictionaryAsync(c => c.Zip, c => c.Id);
            repairedCityIds.Where(c => c.Key != "8000").Should().BeEquivalentTo(existingCityIds);
        }

        using (var factory = CreateFactory())
        {
            using var client = factory.CreateClient();
            var cities = await client.GetFromJsonAsync<List<CityDto>>("/api/cities");
            cities.Should().NotBeNull();
            cities!.ToDictionary(c => c.Zip, c => c.Id).Should().BeEquivalentTo(repairedCityIds);
        }
    }

    private ProductionFactory CreateFactory() => new(directory, AdminUsername, AdminPassword);

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
