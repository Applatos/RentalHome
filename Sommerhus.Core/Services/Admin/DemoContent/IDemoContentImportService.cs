using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.DemoContent;

/// <summary>
/// Creates the demo site's content (features, areas, a house group with its season calendar,
/// and houses with images and prices) from the content folder. Safe to run repeatedly: it
/// creates what is missing and never overwrites houses, areas or prices that already exist.
/// </summary>
public interface IDemoContentImportService
{
    Task<DemoContentImportResultDto> ImportAsync(CancellationToken ct);
}

public sealed class DemoContentOptions
{
    public const string Section = "DemoContent";

    /// <summary>
    /// Folder holding <c>demo-content.json</c> and the files it names. Defaults to
    /// <c>DemoContent</c> beside the application's binaries.
    /// </summary>
    public string? Path { get; set; }

    public string ResolvePath()
        => string.IsNullOrWhiteSpace(Path)
            ? System.IO.Path.Combine(AppContext.BaseDirectory, "DemoContent")
            : Path;
}
