namespace Sommerhus.Api.Infrastructure.Storage;

using System.ComponentModel.DataAnnotations;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    [Required]
    public string UploadsFolder { get; set; } = "uploads";
}
