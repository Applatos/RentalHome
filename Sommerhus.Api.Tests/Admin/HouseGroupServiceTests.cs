using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Repository;
using Xunit;

namespace Sommerhus.Api.Tests.Admin;

public sealed class HouseGroupServiceTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceScopeFactory scopeFactory;

    public HouseGroupServiceTests(CustomWebApplicationFactory factory)
    {
        scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task CreateAsync_WithValidName_CreatesGroup()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminHouseGroupService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var result = await service.CreateAsync(new HouseGroupDto("  Familie "), CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.Label.Should().Be("Familie");

        var exists = await db.HouseGroups.AnyAsync(g => g.Name == "Familie");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_ReturnsConflict()
    {
        using var scope = scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IAdminHouseGroupService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.HouseGroups.Add(new HouseGroup
        {
            Id = Guid.NewGuid(),
            Name = "Nord",
        });
        await db.SaveChangesAsync();

        var result = await service.CreateAsync(new HouseGroupDto("Nord"), CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Conflict);
        result.Errors.Should().ContainKey(nameof(HouseGroupDto.name));
        result.Errors[nameof(HouseGroupDto.name)].Should().Contain("En gruppe med dette navn findes allerede.");
    }
}
