namespace Sommerhus.Api.Infrastructure.Storage;

using System.ComponentModel.DataAnnotations;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string UploadsPath { get; set; } = "uploads";
    public string? LogsPath { get; init; }              // optional
    public string? DbPath { get; init; }                // if SQLite/LocalDB file
}

//
