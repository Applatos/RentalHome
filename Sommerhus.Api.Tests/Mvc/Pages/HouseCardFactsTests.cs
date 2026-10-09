using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Localization;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;
using Sommerhus.Mvc.Infrastructure;

namespace Sommerhus.Api.Tests.Mvc.Pages;

public sealed class HouseCardFactsTests
{
    private static readonly CultureInfo Danish = new("da-DK");

    // The features of "Blåvand Strand 4" in their sort order, as the house list delivers them.
    private static readonly FeatureValueDto[] Features =
    [
        Feature("bedrooms", "Soveværelser", FeatureValueType.Int, FeatureCategory.Property, "5"),
        Feature("bathrooms", "Badeværelser", FeatureValueType.Int, FeatureCategory.Property, "2"),
        Feature("max_guests", "Personer", FeatureValueType.Int, FeatureCategory.Property, "12", icon: "/i/personer.png"),
        Feature("size_m2", "Boligareal", FeatureValueType.Int, FeatureCategory.Property, "225", unit: "m²", icon: "/i/house.png"),
        Feature("distance_water", "Til stranden", FeatureValueType.Int, FeatureCategory.Distance, "100", unit: "m", icon: "/i/beach.png"),
        Feature("pool", "Pool", FeatureValueType.Bool, FeatureCategory.Facility, "true"),
        Feature("spa", "Spa", FeatureValueType.Bool, FeatureCategory.Facility, "true", icon: "/i/spa.png"),
        Feature("sauna", "Sauna", FeatureValueType.Bool, FeatureCategory.Facility, "false", icon: "/i/sauna.png"),
        Feature("water_view", "Havudsigt", FeatureValueType.Bool, FeatureCategory.Distance, "true", icon: "/i/view.png"),
        Feature("wifi", "Trådløst internet", FeatureValueType.Bool, FeatureCategory.Facility, "true", icon: "/i/wifi.png"),
        Feature("tv", "TV", FeatureValueType.Bool, FeatureCategory.Facility, "true", icon: "/i/tv.png"),
    ];

    [Fact]
    public void FactsLine_NamesGuestsBedroomsAndSize()
        => HouseCardFacts.FactsLine(Features, new Texts(), Danish).Should().Be("12 personer · 5 soveværelser · 225 m²");

    [Fact]
    public void RoomsLine_NamesGuestsBedroomsAndBathrooms()
        => HouseCardFacts.RoomsLine(Features, new Texts(), Danish).Should().Be("12 personer · 5 soveværelser · 2 badeværelser");

    [Fact]
    public void Icons_TakeTheFirstFourWithAnIcon_SkippingPropertyFeaturesAndNo()
        => HouseCardFacts.Icons(Features, Danish).Select(i => i.Label)
            .Should().Equal("100 m til stranden", "Spa", "Havudsigt", "Trådløst internet");

    [Fact]
    public void Icons_FormatLargeDistancesInTheCulture()
        => HouseCardFacts.Icons([Feature("distance_water", "To the beach", FeatureValueType.Int, FeatureCategory.Distance, "3000", unit: "m", icon: "/i/beach.png")], new CultureInfo("en-GB"))
            .Single().Label.Should().Be("3,000 m to the beach");

    [Theory]
    [InlineData("da-DK", "DKK", "1.700 kr.")]
    [InlineData("en-GB", "DKK", "DKK 1,700")]
    [InlineData("da-DK", "EUR", "EUR 1.700")]
    public void MoneyText_ReadsAsPeopleWriteIt(string culture, string currency, string expected)
        => MoneyText.Format(1700m, currency, new CultureInfo(culture)).Should().Be(expected);

    private static FeatureValueDto Feature(string key, string name, FeatureValueType type, FeatureCategory category, string value, string? unit = null, string? icon = null)
        => new(Guid.Empty, name, type, unit, icon, value, key, category);

    // The Danish texts the card uses.
    private sealed class Texts : IStringLocalizer
    {
        private static readonly Dictionary<string, string> Values = new()
        {
            ["Pages.HouseCard.Guests"] = "{0} personer",
            ["Pages.HouseCard.Bedrooms"] = "{0} soveværelser",
            ["Pages.House.Bathrooms"] = "{0} badeværelser",
        };

        public LocalizedString this[string name] => new(name, Values[name]);

        public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(Values[name], arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
