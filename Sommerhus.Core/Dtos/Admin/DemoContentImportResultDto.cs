namespace Sommerhus.Core.Dtos.Admin;

/// <summary>
/// What a demo content import did. <see cref="Changes"/> lists what it created or updated, even
/// when it stopped on <see cref="Error"/>; <see cref="Notes"/> lists what it left alone and why.
/// </summary>
public sealed record DemoContentImportResultDto(
    bool Succeeded,
    IReadOnlyList<string> Changes,
    IReadOnlyList<string> Notes,
    string? Error);
