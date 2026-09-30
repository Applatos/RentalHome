using System.Globalization;
using FluentAssertions;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class HouseFeatureFactsTests
{
    private static readonly CultureInfo Danish = new("da-DK");

    private static FeatureValueDto Feature(string name, FeatureValueType type, string raw, string? unit = null, string? icon = null)
        => new(Guid.NewGuid(), name, type, unit, icon, raw);

    [Fact]
    public void From_SeededHouse_ShowsFactsWithValuesAndOnlyTheFacilitiesItHas()
    {
        var features = new[]
        {
            Feature("Bedrooms", FeatureValueType.Int, "3"),
            Feature("Max guests", FeatureValueType.Int, "6"),
            Feature("Size (m²)", FeatureValueType.Int, "210", unit: "m²"),
            Feature("Swimming pool", FeatureValueType.Bool, "true", icon: "/icons/pool.svg"),
            Feature("Pet friendly", FeatureValueType.Bool, "false"),
            Feature("Distance to shop", FeatureValueType.Int, "1800", unit: "m"),
            Feature("Sauna", FeatureValueType.Bool, "1")
        };

        var facts = HouseFeatureFacts.From(features, Danish);

        facts.Facts.Should().Equal(
            new HouseFact("3", "Bedrooms", null),
            new HouseFact("6", "Max guests", null),
            new HouseFact("210 m²", "Size", null),
            new HouseFact("1.800 m", "Distance to shop", null));
        facts.Facilities.Should().Equal(
            new HouseFacility("Swimming pool", "/icons/pool.svg"),
            new HouseFacility("Sauna", null));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("no")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("maybe")]
    public void From_BoolThatIsNotYes_IsLeftOut(string raw)
        => HouseFeatureFacts.From([Feature("Pet friendly", FeatureValueType.Bool, raw)], Danish).IsEmpty.Should().BeTrue();

    [Fact]
    public void From_EmptyValue_IsLeftOut()
        => HouseFeatureFacts.From([Feature("Bedrooms", FeatureValueType.Int, " ")], Danish).IsEmpty.Should().BeTrue();

    [Fact]
    public void From_Decimal_IsFormattedInTheCurrentCulture()
    {
        var features = new[] { Feature("Plot (ha)", FeatureValueType.Decimal, "1.25", unit: "ha") };

        HouseFeatureFacts.From(features, Danish).Facts.Single().Should().Be(new HouseFact("1,25 ha", "Plot", null));
        HouseFeatureFacts.From(features, new CultureInfo("en-GB")).Facts.Single().Value.Should().Be("1.25 ha");
    }

    [Fact]
    public void From_DecimalWithLocalComma_IsNotReadAsThousands()
        => HouseFeatureFacts.From([Feature("Plot", FeatureValueType.Decimal, "12,5")], Danish)
            .Facts.Single().Value.Should().Be("12,5");

    [Fact]
    public void From_TextFeature_KeepsItsTextAndNoUnit()
        => HouseFeatureFacts.From([Feature("Heating", FeatureValueType.Text, " Heat pump ", unit: "kW")], Danish)
            .Facts.Single().Should().Be(new HouseFact("Heat pump", "Heating", null));

    [Fact]
    public void From_UnitNotInTheName_KeepsTheName()
        => HouseFeatureFacts.From([Feature("Distance to water", FeatureValueType.Int, "200", unit: "m")], Danish)
            .Facts.Single().Caption.Should().Be("Distance to water");

    [Fact]
    public void From_NoFeatures_IsEmpty()
        => HouseFeatureFacts.From(null, Danish).IsEmpty.Should().BeTrue();
}
