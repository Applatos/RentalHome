namespace Sommerhus.Core.Common;

/// <summary>
/// The stored form of a season code: trimmed and upper-case, at most <see cref="MaxLength"/> characters.
/// </summary>
public static class SeasonCodeFormat
{
    public const int MaxLength = 10;

    /// <summary>
    /// Trims and upper-cases a code; null becomes an empty string.
    /// </summary>
    public static string Normalize(string? code) => (code ?? string.Empty).Trim().ToUpperInvariant();
}
