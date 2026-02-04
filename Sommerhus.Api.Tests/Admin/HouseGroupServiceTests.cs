using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core.Services.Admin.HouseGroups;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Core;
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

        var result = await service.CreateAsync(new HouseGroupDto(Guid.NewGuid(), "  Familie "), CancellationToken.None);

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

        var result = await service.CreateAsync(new HouseGroupDto(Guid.NewGuid(), "Nord"), CancellationToken.None);

        result.Status.Should().Be(ServiceResultStatus.Conflict);
        result.Errors.Should().ContainKey(nameof(HouseGroupDto.Name));
        result.Errors[nameof(HouseGroupDto.Name)].Should().Contain("A group with this name already exists.");
    }
}
