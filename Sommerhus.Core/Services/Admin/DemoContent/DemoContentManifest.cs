using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace Sommerhus.Core.Services.Admin.DemoContent;

/// <summary>
/// The demo content the import creates, read from <c>demo-content.json</c> in the content folder.
/// Text and image paths are relative to that folder.
/// </summary>
public sealed record DemoContentManifest(
    IReadOnlyList<DemoSeasonCode> SeasonCodes,
    DemoHouseGroup Group,
    IReadOnlyList<DemoFeature> Features,
    IReadOnlyList<DemoArea> Areas,
    IReadOnlyList<DemoHouse> Houses)
{
    public const string FileName = "demo-content.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static DemoContentManifest Load(string contentRoot)
    {
        var path = Path.Combine(contentRoot, FileName);
        if (!File.Exists(path))
            throw new DemoContentException($"Demo content not found: {path}");

        var manifest = JsonSerializer.Deserialize<DemoContentManifest>(File.ReadAllText(path), JsonOptions)
            ?? throw new DemoContentException($"{FileName} is empty.");

        // Deserialization leaves a missing list null despite the declared types.
        if (manifest.SeasonCodes is null || manifest.Group?.Seasons is null || manifest.Features is null
            || manifest.Areas is null || manifest.Houses is null)
            throw new DemoContentException($"{FileName} must define seasonCodes, group, features, areas and houses.");

        return manifest;
    }
}

public sealed record DemoSeasonCode(string Code, string Name, string Color, int SortOrder);

/// <param name="Years">How many calendar years the group's calendar covers, starting with the current one.</param>
public sealed record DemoHouseGroup(string Name, int Years, IReadOnlyList<DemoSeason> Seasons);

/// <param name="From">First night of the season as MM-dd.</param>
/// <param name="To">Last night of the season as MM-dd.</param>
public sealed record DemoSeason(string Code, string From, string To);

/// <param name="Icon">File name in the content folder's <c>features</c> directory.</param>
public sealed record DemoFeature(
    string Key,
    string Name,
    string? NameEn,
    string ValueType,
    string Category,
    string? Unit,
    string? Options,
    string? Icon,
    int SortOrder);

public sealed record DemoArea(
    Guid Id,
    string Name,
    string? NameEn,
    IReadOnlyList<string>? Zips,
    string Text,
    string? ImageFolder);

/// <param name="Areas">Area names; each must be in the manifest or already in the database.</param>
/// <param name="Features">Raw values by feature key.</param>
/// <param name="Prices">Nightly price including VAT by season code.</param>
public sealed record DemoHouse(
    Guid Id,
    string Title,
    string? Address,
    string Zip,
    IReadOnlyList<string>? Areas,
    string Text,
    string? SearchKeywords,
    string ImageFolder,
    string? Cover,
    string? Floorplan,
    IReadOnlyList<string>? SkipImages,
    IReadOnlyDictionary<string, string> Features,
    IReadOnlyDictionary<string, decimal> Prices);

/// <summary>
/// Reads the content folder's text and image files.
/// </summary>
public static class DemoContentFiles
{
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png"];

    /// <summary>
    /// Converts a text file to the HTML the site renders descriptions as. A blank line starts a
    /// new paragraph and a single line break is kept as <c>&lt;br&gt;</c>. A leading one-line
    /// heading ("Beskrivelse", or the entity's own name) is dropped, because the page already
    /// shows one.
    /// </summary>
    public static string ReadDescriptionHtml(string contentRoot, string relativePath, string ownName)
    {
        var path = Resolve(contentRoot, relativePath);
        if (!File.Exists(path))
            throw new DemoContentException($"Text file not found: {relativePath}");

        var paragraphs = Regex.Split(File.ReadAllText(path).Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Select(block => block.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0).ToList())
            .Where(lines => lines.Count > 0)
            .ToList();

        if (paragraphs.Count > 0 && paragraphs[0].Count == 1
            && (paragraphs[0][0].Equals("Beskrivelse", StringComparison.OrdinalIgnoreCase)
                || paragraphs[0][0].Equals(ownName, StringComparison.OrdinalIgnoreCase)))
        {
            paragraphs.RemoveAt(0);
        }

        return string.Join("\n", paragraphs.Select(lines => $"<p>{string.Join("<br>", lines.Select(Encoder.Encode))}</p>"));
    }

    /// <summary>
    /// The image files in a folder, ordered by their number ("2.jpg" before "10.jpg"), then by
    /// name. A missing folder has no images.
    /// </summary>
    public static IReadOnlyList<string> ListImages(string contentRoot, string? relativeFolder, IEnumerable<string>? skip = null)
    {
        if (string.IsNullOrWhiteSpace(relativeFolder))
            return [];

        var folder = Resolve(contentRoot, relativeFolder);
        if (!Directory.Exists(folder))
            return [];

        var skipped = new HashSet<string>(skip ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        return Directory.GetFiles(folder)
            .Where(f => ImageExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Where(f => !skipped.Contains(Path.GetFileName(f)))
            .OrderBy(f => int.TryParse(Path.GetFileNameWithoutExtension(f), out var n) ? n : int.MaxValue)
            .ThenBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string Resolve(string contentRoot, string relativePath)
        => Path.Combine(contentRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    public static string ContentTypeFor(string path)
        => Path.GetExtension(path).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
}
