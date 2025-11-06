using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Storage;
using Sommerhus.Domain.Models;

namespace Sommerhus.Repository.Admin.Images;

public abstract class AdminImageServiceBase
{
    protected AdminImageServiceBase(AppDbContext db, IImageStorage storage)
    {
        Db = db;
        Storage = storage;
    }

    protected AppDbContext Db { get; }

    protected IImageStorage Storage { get; }

    protected static bool IsValidImageFile(IFormFile file)
        => !string.IsNullOrWhiteSpace(file.ContentType)
           && file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
           && file.Length > 0;

    protected string BuildUrl(HttpRequest request, ImageCategory category, Guid ownerId, string fileName)
        => Storage.GetUrl(request, category, ownerId, fileName);

    protected static string? NormalizeAlt(string? alt)
        => string.IsNullOrWhiteSpace(alt) ? null : alt.Trim();

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
}
