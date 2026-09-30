using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Sommerhus.Api.Tests.Infrastructure;
using Sommerhus.Core;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models.Pricing;
using static Sommerhus.Api.Tests.Infrastructure.PricingScenario;

namespace Sommerhus.Api.Tests.Admin;

public sealed class PricePlanTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory factory;
    private readonly PricingScenario scenario;

    public PricePlanTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        scenario = new PricingScenario(factory, factory.CreateAuthenticatedClient());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task UpsertPricing_PriceNotAboveZero_ReturnsBadRequest(int price)
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);

        using var res = await scenario.PutPlanAsync(houseId, new() { ["A"] = HighRate, ["B"] = price });

        (await ReadValidationErrorsAsync(res, "seasonPrices")).Single().Should().Contain("'B'").And.Contain("greater than zero");
        (await GetPlansAsync(houseId)).Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertPricing_DuplicateAndUnknownCodes_ReturnsEveryError()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);

        using var res = await scenario.PutPlanAsync(houseId, new() { ["A"] = HighRate, [" a "] = LowRate, ["ZZ"] = LowRate });

        var errors = await ReadValidationErrorsAsync(res, "seasonPrices");
        errors.Should().HaveCount(2);
        errors.Should().Contain(e => e.Contains("'A' is listed more than once"));
        errors.Should().Contain(e => e.Contains("Unknown season code: ZZ"));
    }

    [Fact]
    public async Task UpsertPricing_LowercaseCodes_AreStoredUpperCase()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);

        await scenario.SavePlanAsync(houseId, new() { [" a"] = HighRate, ["b "] = LowRate });

        var plan = (await GetPlansAsync(houseId)).Single();
        plan.SeasonPrices.Select(p => p.Code).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public async Task InactivePlan_IsShownInDetailsAndSavingUpdatesIt()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate }, isActive: false);

        var details = await scenario.Admin.GetFromJsonAsync<AdminHouseDetailsDto>($"api/admin/houses/{houseId}");
        details!.Pricing.Should().NotBeNull();
        details.Pricing!.IsActive.Should().BeFalse();
        details.Pricing.SeasonPrices.Single().NightlyPrice.Should().Be(HighRate);

        // Saving again, with or without the plan id the form carries, updates that one plan.
        await scenario.SavePlanAsync(houseId, new() { ["A"] = 1100m }, isActive: false, planId: details.Pricing.PlanId);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = 1200m }, isActive: true);

        var plan = (await GetPlansAsync(houseId)).Should().ContainSingle().Subject;
        plan.PlanId.Should().Be(details.Pricing.PlanId);
        plan.IsActive.Should().BeTrue();
        plan.SeasonPrices.Single().NightlyPrice.Should().Be(1200m);
    }

    [Fact]
    public async Task SavingActivePlan_DeactivatesTheHousesOtherPlans()
    {
        var houseId = await scenario.CreateHouseAsync(groupId: null);
        await scenario.SavePlanAsync(houseId, new() { ["A"] = HighRate });
        var saved = (await GetPlansAsync(houseId)).Single();
        var legacyId = await SeedActivePlanAsync(houseId);

        await scenario.SavePlanAsync(houseId, new() { ["A"] = 1300m }, planId: saved.PlanId);

        var plans = await GetPlansAsync(houseId);
        plans.Should().HaveCount(2);
        plans.Should().ContainSingle(p => p.IsActive).Which.PlanId.Should().Be(saved.PlanId);
        plans.Single(p => p.PlanId == legacyId).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task CreateSeasonCode_NormalisesAndRejectsDuplicatesAndLongCodes()
    {
        using var created = await scenario.Admin.PostAsJsonAsync("api/admin/pricing/season-codes", new SeasonCodeDto(" x1 ", "Mellemsæson", null, 5));
        created.StatusCode.Should().Be(HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
        (await created.Content.ReadFromJsonAsync<SeasonCodeDto>())!.Code.Should().Be("X1");

        using var duplicate = await scenario.Admin.PostAsJsonAsync("api/admin/pricing/season-codes", new SeasonCodeDto("x1", null, null, 0));
        using var existing = await scenario.Admin.PostAsJsonAsync("api/admin/pricing/season-codes", new SeasonCodeDto("a", null, null, 0));
        using var tooLong = await scenario.Admin.PostAsJsonAsync("api/admin/pricing/season-codes", new SeasonCodeDto("ABCDEFGHIJK", null, null, 0));
        using var badColor = await scenario.Admin.PostAsJsonAsync("api/admin/pricing/season-codes", new SeasonCodeDto("X2", null, "blue", 0));

        (await ReadValidationErrorsAsync(duplicate, "Code")).Single().Should().Contain("already exists");
        (await ReadValidationErrorsAsync(existing, "Code")).Single().Should().Contain("'A' already exists");
        await ReadValidationErrorsAsync(tooLong, "Code");
        await ReadValidationErrorsAsync(badColor, "Color");
    }

    private async Task<List<PricePlanDetailsDto>> GetPlansAsync(Guid houseId)
        => (await scenario.Admin.GetFromJsonAsync<List<PricePlanDetailsDto>>($"api/admin/pricing/plans/{houseId}"))!;

    /// <summary>
    /// A second active plan, as data from before "at most one active plan" was enforced.
    /// </summary>
    private async Task<Guid> SeedActivePlanAsync(Guid houseId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var plan = new PricePlan { HouseId = houseId, Name = "Legacy", Currency = "DKK", IsActive = true };
        plan.SeasonPrices.Add(new SeasonPrice { Code = "A", NightlyPrice = 500m });
        db.PricePlans.Add(plan);
        await db.SaveChangesAsync();
        return plan.Id;
    }
}
