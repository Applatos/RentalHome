using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Images;

/// <summary>
/// Base class for admin image services providing common functionality.
/// </summary>
public abstract class AdminImageServiceBase(AppDbContext db, IImageStorage storage)
{
    protected AppDbContext Db { get; } = db;

    protected IImageStorage Storage { get; } = storage;

    /// <summary>
    /// Validates that the file is a valid image file.
    /// </summary>
    protected static bool IsValidImageFile(IFormFile file)
        => !string.IsNullOrWhiteSpace(file.ContentType)
           && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
           && file.Length > 0;

    /// <summary>
    /// Validates a single file and returns an error result if invalid.
    /// </summary>
    protected static ServiceResult<T>? ValidateSingleFile<T>(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return ServiceResult<T>.Invalid("file", "Image file is required.");

        if (!IsValidImageFile(file))
            return ServiceResult<T>.Invalid("file", "Only image files are allowed.");

        return null; // Valid
    }

    /// <summary>
    /// Validates a file collection and returns an error result if invalid.
    /// </summary>
    protected static ServiceResult<IReadOnlyList<ImageDto>>? ValidateFileCollection(IFormFileCollection? files)
    {
        if (files is null || files.Count == 0)
            return ServiceResult<IReadOnlyList<ImageDto>>.Invalid("files", "At least one image file must be provided.");

        return null; // Valid - individual files checked during processing
    }

    /// <summary>
    /// Builds a URL for the image.
    /// </summary>
    protected string BuildUrl(string baseUrl, ImageCategory category, Guid ownerId, string fileName)
        => Storage.GetUrl(baseUrl, category, ownerId, fileName);

    /// <summary>
    /// Creates an ImageDto from image properties.
    /// </summary>
    protected ImageDto ToDto(Guid id, string baseUrl, ImageCategory category, Guid ownerId, string fileName, string? alt, string kind)
        => new(id, BuildUrl(baseUrl, category, ownerId, fileName), alt, kind);

    /// <summary>
    /// Normalizes alt text by trimming whitespace.
    /// </summary>
    protected static string? NormalizeAlt(string? alt)
        => string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();

    /// <summary>
    /// Clears exclusive image kinds (like Cover or Floorplan) from other images.
    /// </summary>
    protected static void ClearExclusiveKind(IEnumerable<HouseImage> images, ImageKind kind, Guid keepId)
    {
        foreach (var image in images)
        {
            if (image.Kind == kind && image.Id != keepId)
            {
                image.Kind = ImageKind.Gallery;
            }
        }
    }

    /// <summary>
    /// Deletes an image file from storage.
    /// </summary>
    protected async Task DeleteFromStorageAsync(ImageCategory category, Guid ownerId, string fileName, CancellationToken ct)
    {
        await Storage.DeleteAsync(category, ownerId, fileName, ct);
    }

    /// <summary>
    /// Saves an image file to storage.
    /// </summary>
    protected async Task<StoredImage> SaveToStorageAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct)
    {
        return await Storage.SaveAsync(category, ownerId, file, ct);
    }
}
