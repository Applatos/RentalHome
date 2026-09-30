using System.Collections;
using System.Globalization;
using System.Resources;
using FluentAssertions;
using Sommerhus.Mvc;

namespace Sommerhus.Api.Tests.Mvc.Pages;

/// <summary>
/// The texts of the house page, the booking form and the admin house tabs exist in Danish and
/// English alike, so a missing translation shows up here rather than as a key on the page.
/// </summary>
public sealed class PageResourcesTests
{
    private static readonly ResourceManager Resources = new("Sommerhus.Mvc.Resources.SharedResource", typeof(SharedResource).Assembly);

    private static Dictionary<string, string> PageTexts(CultureInfo culture)
        => Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .Where(e => ((string)e.Key).StartsWith("Pages.", StringComparison.Ordinal))
            .ToDictionary(e => (string)e.Key, e => (string)e.Value!);

    [Fact]
    public void PageKeys_ExistInDanishAndEnglish()
    {
        var danish = PageTexts(CultureInfo.InvariantCulture);
        var english = PageTexts(new CultureInfo("en"));

        danish.Should().NotBeEmpty();
        english.Keys.Should().BeEquivalentTo(danish.Keys);
        danish.Values.Should().OnlyContain(v => !string.IsNullOrWhiteSpace(v));
        english.Values.Should().OnlyContain(v => !string.IsNullOrWhiteSpace(v));
    }

    [Fact]
    public void PageKeys_KeepTheirPlaceholdersInBothLanguages()
    {
        var danish = PageTexts(CultureInfo.InvariantCulture);
        var english = PageTexts(new CultureInfo("en"));

        foreach (var (key, text) in danish)
        {
            Placeholders(english[key]).Should().BeEquivalentTo(Placeholders(text), $"'{key}' must fill the same placeholders");
        }
    }

    private static IEnumerable<string> Placeholders(string text)
        => System.Text.RegularExpressions.Regex.Matches(text, @"\{\w+\}").Select(m => m.Value).Distinct();
}
