using System.Text.Json;

namespace Sommerhus.Core.Data;

internal static class DanishGeoData
{
    public static IReadOnlyList<DanishPostalDistrict> Load()
    {
        using var stream = typeof(DanishGeoData).Assembly.GetManifestResourceStream(
            "Sommerhus.Core.Data.DanishGeoData.json")
            ?? throw new InvalidOperationException("The bundled Danish postal districts are missing.");

        var entries = JsonSerializer.Deserialize<List<DanishPostalDistrict>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (entries is null || entries.Count == 0 ||
            entries.Any(entry => string.IsNullOrWhiteSpace(entry.Nr) || string.IsNullOrWhiteSpace(entry.Navn)))
        {
            throw new InvalidOperationException("The bundled Danish postal districts are empty or invalid.");
        }

        return entries;
    }
}

internal sealed record DanishPostalDistrict(string Nr, string Navn, string? Kommune);
