using System.Globalization;
using FluentAssertions;
using Sommerhus.Mvc.ModelBinding;

namespace Sommerhus.Api.Tests.Mvc;

public sealed class CultureSafeNumberTests
{
    [Theory]
    // What <input type="number"> posts, whatever the page's culture.
    [InlineData("800.5", "da-DK", "800.5")]
    [InlineData("1250.00", "da-DK", "1250")]
    [InlineData("800.5", "en-GB", "800.5")]
    // Only a comma: the comma is the decimal separator.
    [InlineData("800,5", "da-DK", "800.5")]
    [InlineData("800,5", "en-GB", "800.5")]
    // Both: the current culture says which one groups thousands.
    [InlineData("1.250,50", "da-DK", "1250.5")]
    [InlineData("1,250.50", "en-GB", "1250.5")]
    // Neither: plain digits.
    [InlineData("1250", "da-DK", "1250")]
    [InlineData("1250", "en-GB", "1250")]
    [InlineData("-3.25", "da-DK", "-3.25")]
    [InlineData(" 12.5 ", "da-DK", "12.5")]
    [InlineData("0", "da-DK", "0")]
    public void TryParseDecimal_ReadsTheNumberThatWasMeant(string input, string culture, string expected)
    {
        CultureSafeNumber.TryParseDecimal(input, new CultureInfo(culture), out var value).Should().BeTrue();

        value.Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("1,2,3")]
    // Both separators, in the order the current culture does not use.
    [InlineData("1,250.50")]
    public void TryParseDecimal_RejectsWhatIsNotANumber(string? input)
    {
        CultureSafeNumber.TryParseDecimal(input, new CultureInfo("da-DK"), out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("da-DK")]
    [InlineData("en-GB")]
    public void TryParseDecimal_WithoutCulture_UsesTheCurrentCulture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            var input = culture == "da-DK" ? "1.250,5" : "1,250.5";

            CultureSafeNumber.TryParseDecimal("800.5", out var browserValue).Should().BeTrue();
            CultureSafeNumber.TryParseDecimal(input, out var groupedValue).Should().BeTrue();

            browserValue.Should().Be(800.5m);
            groupedValue.Should().Be(1250.5m);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("0.1", "da-DK")]
    [InlineData("0,1", "da-DK")]
    [InlineData("0.1", "en-GB")]
    [InlineData("0,1", "en-GB")]
    public void TryParseDoubleAndSingle_FollowTheSameRules(string input, string culture)
    {
        CultureSafeNumber.TryParseDouble(input, new CultureInfo(culture), out var dbl).Should().BeTrue();
        CultureSafeNumber.TryParseSingle(input, new CultureInfo(culture), out var flt).Should().BeTrue();

        dbl.Should().Be(0.1d);
        flt.Should().Be(0.1f);
    }

    [Theory]
    [InlineData(typeof(decimal), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(int), false)]
    [InlineData(typeof(string), false)]
    public void TryParse_ByType_ParsesOnlyFloatingPointTypes(Type type, bool supported)
    {
        var parsed = CultureSafeNumber.TryParse("2.5", type, new CultureInfo("da-DK"), out var value);

        parsed.Should().Be(supported);
        if (supported)
        {
            value.Should().BeOfType(type);
            Convert.ToDecimal(value, CultureInfo.InvariantCulture).Should().Be(2.5m);
        }
    }
}
