using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using FluentAssertions;
using Sommerhus.Mvc;

namespace Sommerhus.Api.Tests.Mvc.Pages;

/// <summary>
/// Every text of the site exists in Danish and English alike, with the same placeholders, so a
/// missing translation shows up here rather than as a key on the page. Danish is the default
/// language, so its values must also be written with æ, ø and å.
/// </summary>
public sealed class PageResourcesTests
{
    private static readonly ResourceManager Resources = new("Sommerhus.Mvc.Resources.SharedResource", typeof(SharedResource).Assembly);

    // The ASCII spellings Danish texts used before they were written with æ, ø and å. A word that
    // is Danish on its own ("datoer") is not listed.
    private static readonly Regex AsciiSubstitutes = new(
        @"\b(paa|naar|goer|saet|laes\w*|vaelg\w*)\b|omraad|soeg|gaest|naet|naes|gaeng|saeson|traek|noegle|foerst|faelles|aendr|opsaet|genabn",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static Dictionary<string, string> AllTexts(CultureInfo culture)
        => Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!
            .Cast<DictionaryEntry>()
            .ToDictionary(e => (string)e.Key, e => (string)e.Value!);

    private static Dictionary<string, string> PageTexts(CultureInfo culture)
        => AllTexts(culture)
            .Where(e => e.Key.StartsWith("Pages.", StringComparison.Ordinal))
            .ToDictionary(e => e.Key, e => e.Value);

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

    [Fact]
    public void AllKeys_ExistInDanishAndEnglishWithText()
    {
        var danish = AllTexts(CultureInfo.InvariantCulture);
        var english = AllTexts(new CultureInfo("en"));

        danish.Should().NotBeEmpty();
        english.Keys.Should().BeEquivalentTo(danish.Keys);
        danish.Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key).Should().BeEmpty("every Danish text must be filled in");
        english.Where(e => string.IsNullOrWhiteSpace(e.Value)).Select(e => e.Key).Should().BeEmpty("every English text must be filled in");
    }

    [Fact]
    public void AllKeys_KeepTheirPlaceholdersInBothLanguages()
    {
        var danish = AllTexts(CultureInfo.InvariantCulture);
        var english = AllTexts(new CultureInfo("en"));

        foreach (var (key, text) in danish)
        {
            Placeholders(english[key]).Should().BeEquivalentTo(Placeholders(text), $"'{key}' must fill the same placeholders");
        }
    }

    [Fact]
    public void DanishTexts_UseDanishLettersNotAsciiSubstitutes()
    {
        var offenders = AllTexts(CultureInfo.InvariantCulture)
            .Where(e => AsciiSubstitutes.IsMatch(e.Value))
            .Select(e => $"{e.Key}: {e.Value}");

        offenders.Should().BeEmpty();
    }

    private static IEnumerable<string> Placeholders(string text)
        => Regex.Matches(text, @"\{\w+\}").Select(m => m.Value).Distinct();
}
