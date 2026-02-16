using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Api.Tests.Public;

public sealed class LocalizationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LocalizationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Areas_AcceptLanguageEnglish_UsesEnglishNameWithFallback()
    {
        Guid areaWithTranslation;
        Guid areaWithoutTranslation;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var areas = db.Areas.OrderBy(a => a.Name).Take(2).ToList();

            if (areas.Count < 2)
            {
                var extra = new Area
                {
                    Id = Guid.NewGuid(),
                    Name = "Vejers",
                    NameEn = null,
                    Description = "Extra test area",
                    Status = EntityStatus.Published
                };
                db.Areas.Add(extra);
                await db.SaveChangesAsync();
                areas = db.Areas.OrderBy(a => a.Name).Take(2).ToList();
            }

            areas.Should().HaveCountGreaterThanOrEqualTo(2);

            areas[0].Name = "Blaavand";
            areas[0].NameEn = "Blavand";
            areas[0].Status = EntityStatus.Published;
            areas[1].Name = "Henne Strand";
            areas[1].NameEn = null;
            areas[1].Status = EntityStatus.Published;

            await db.SaveChangesAsync();

            areaWithTranslation = areas[0].Id;
            areaWithoutTranslation = areas[1].Id;
        }

        _client.DefaultRequestHeaders.AcceptLanguage.Clear();
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB");

        var items = await _client.GetFromJsonAsync<List<AreaListItemDto>>("/api/areas");

        items.Should().NotBeNull();
        items!.Should().Contain(a => a.Id == areaWithTranslation && a.Name == "Blavand");
        items.Should().Contain(a => a.Id == areaWithoutTranslation && a.Name == "Henne Strand");
    }

    [Fact]
    public async Task Features_AcceptLanguageEnglish_UsesEnglishName()
    {
        Guid featureId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var feature = db.Features.First();
            feature.Name = "Pool";
            feature.NameEn = "Swimming pool";
            await db.SaveChangesAsync();
            featureId = feature.Id;
        }

        _client.DefaultRequestHeaders.AcceptLanguage.Clear();
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-GB");

        var features = await _client.GetFromJsonAsync<List<FeatureDto>>("/api/features");

        features.Should().NotBeNull();
        features!.Should().Contain(f => f.Id == featureId && f.Name == "Swimming pool");
    }

    [Fact]
    public async Task Areas_DefaultCulture_UsesDanishName()
    {
        Guid areaId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var area = db.Areas.First();
            area.Name = "Blaavand";
            area.NameEn = "Blavand";
            area.Status = EntityStatus.Published;
            await db.SaveChangesAsync();
            areaId = area.Id;
        }

        _client.DefaultRequestHeaders.AcceptLanguage.Clear();

        var items = await _client.GetFromJsonAsync<List<AreaListItemDto>>("/api/areas");

        items.Should().NotBeNull();
        items!.Should().Contain(a => a.Id == areaId && a.Name == "Blaavand");
    }
}
