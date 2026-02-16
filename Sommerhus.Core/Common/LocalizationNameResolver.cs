using System.Globalization;

namespace Sommerhus.Core.Common;

public static class LocalizationNameResolver
{
    public static string Resolve(string defaultName, string? englishName)
    {
        if (string.Equals(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "en", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(englishName))
        {
            return englishName.Trim();
        }

        return defaultName;
    }
}
