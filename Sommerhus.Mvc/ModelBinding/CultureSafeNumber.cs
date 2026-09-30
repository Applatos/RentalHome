using System.Globalization;

namespace Sommerhus.Mvc.ModelBinding;

/// <summary>
/// Reads decimal numbers that come from browsers and people, whatever the request culture.
/// </summary>
/// <remarks>
/// The default request culture is da-DK, where "." groups thousands, but an
/// <c>&lt;input type="number"&gt;</c> always posts "800.5". Parsing that with the request culture
/// turns 800.5 into 8005. The separators in the value decide instead:
/// <list type="bullet">
/// <item>both "," and "." — the current culture decides which one groups thousands ("1.250,50" in da-DK);</item>
/// <item>only "," — the comma is the decimal separator ("800,5");</item>
/// <item>otherwise — invariant culture ("800.5", "1250").</item>
/// </list>
/// </remarks>
public static class CultureSafeNumber
{
    // Same styles as MVC's own decimal and floating-point binders.
    private const NumberStyles Styles = NumberStyles.Float | NumberStyles.AllowThousands;

    private static readonly NumberFormatInfo CommaDecimal = CreateCommaDecimal();

    public static bool TryParseDecimal(string? value, out decimal result)
        => TryParseDecimal(value, CultureInfo.CurrentCulture, out result);

    public static bool TryParseDecimal(string? value, CultureInfo culture, out decimal result)
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && decimal.TryParse(value, Styles, FormatFor(value, culture), out result);
    }

    public static bool TryParseDouble(string? value, CultureInfo culture, out double result)
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && double.TryParse(value, Styles, FormatFor(value, culture), out result);
    }

    public static bool TryParseSingle(string? value, CultureInfo culture, out float result)
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value)
            && float.TryParse(value, Styles, FormatFor(value, culture), out result);
    }

    /// <summary>Parses <paramref name="value"/> as decimal, double or float.</summary>
    public static bool TryParse(string? value, Type type, CultureInfo culture, out object? result)
    {
        result = null;
        if (type == typeof(decimal) && TryParseDecimal(value, culture, out var dec))
            result = dec;
        else if (type == typeof(double) && TryParseDouble(value, culture, out var dbl))
            result = dbl;
        else if (type == typeof(float) && TryParseSingle(value, culture, out var flt))
            result = flt;
        return result is not null;
    }

    public static bool IsSupported(Type type)
        => type == typeof(decimal) || type == typeof(double) || type == typeof(float);

    /// <summary>The number format a value is read with, chosen by the separators it contains.</summary>
    public static IFormatProvider FormatFor(string value, CultureInfo culture)
    {
        var hasComma = value.Contains(',');
        var hasDot = value.Contains('.');

        if (hasComma && hasDot)
            return culture;
        return hasComma ? CommaDecimal : CultureInfo.InvariantCulture;
    }

    private static NumberFormatInfo CreateCommaDecimal()
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        format.NumberDecimalSeparator = ",";
        // The group separator must differ from the decimal separator. A value that reaches this
        // format holds no "." at all, so a dot can never be taken for grouping here.
        format.NumberGroupSeparator = ".";
        return NumberFormatInfo.ReadOnly(format);
    }
}
