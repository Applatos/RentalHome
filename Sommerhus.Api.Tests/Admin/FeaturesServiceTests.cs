using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Core;
using Xunit;

namespace Sommerhus.Api.Tests.Admin;

public sealed class FeaturesServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceScopeFactory scopeFactory;

    public FeaturesServiceTests(CustomWebApplicationFactory factory)
    {
        scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task CreateAsync_WithValidPayload_PersistsFeature()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminFeatureService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var dto = new UpsertFeatureDto("Pool", " pool_key ", "Bool", Unit: " ");

        var result = await service.CreateAsync(dto, CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Success);
        result.Value.Should().NotBe(Guid.Empty);

        var stored = await db.Features.SingleAsync(f => f.Id == result.Value);
        stored.Name.Should().Be("Pool");
        stored.Key.Should().Be("pool_key");
        stored.ValueType.Should().Be(FeatureValueType.Bool);
        stored.Unit.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateKey_ReturnsInvalid()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminFeatureService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var uniqueKey = $"duptest_{Guid.NewGuid():N}"[..30];
        db.Features.Add(new Feature
        {
            Id = Guid.NewGuid(),
            Name = "Dup Test Feature",
            Key = uniqueKey,
            ValueType = FeatureValueType.Bool,
        });
        await db.SaveChangesAsync();

        var dto = new UpsertFeatureDto("Dup Test Feature 2", uniqueKey, "Bool");

        var result = await service.CreateAsync(dto, CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Invalid);
        result.Errors.Should().ContainKey(nameof(UpsertFeatureDto.Key));
        result.Errors[nameof(UpsertFeatureDto.Key)].Should().Contain("Key is already in use.");
    }
}
