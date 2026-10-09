using System.Globalization;

namespace Sommerhus.Mvc.Infrastructure;

/// <summary>
/// A price as people read it: "1.700 kr." in Danish, "DKK 1,700" in English. Whole amounts
/// only, which is how the site shows nightly prices.
/// </summary>
public static class MoneyText
{
    public static string Format(decimal amount, string? currency, CultureInfo culture)
    {
        var number = amount.ToString("N0", culture);
        var code = string.IsNullOrWhiteSpace(currency) ? "DKK" : currency.Trim().ToUpperInvariant();

        return culture.TwoLetterISOLanguageName == "da" && code == "DKK"
            ? $"{number} kr."
            : $"{code} {number}";
    }
}
