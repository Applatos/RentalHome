using Microsoft.AspNetCore.Http;

namespace Sommerhus.Core.Services.Storage;

public enum ImageCategory
{
    Area,
    City,
    House,
    Feature,
}

public sealed record StoredImage(string FileName, string RelativePath);

public interface IImageStorage
{
    Task<StoredImage> SaveAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct);
    Task DeleteAsync(ImageCategory category, Guid ownerId, string fileName, CancellationToken ct);
    Task DeleteAsync(string? relativePath, CancellationToken ct);
    string GetUrl(HttpRequest request, ImageCategory category, Guid ownerId, string fileName);
    string? GetUrl(HttpRequest request, string? relativePath);
    string GetRelativePath(ImageCategory category, Guid ownerId, string fileName);
}
