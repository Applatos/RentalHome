namespace Sommerhus.Api.Infrastructure.Storage;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Options;
using Sommerhus.Core.Services.Storage;

public sealed class PhysicalImageStorage : IImageStorage
{
    private readonly IWebHostEnvironment environment;
    private readonly StorageOptions options;

    public PhysicalImageStorage(IWebHostEnvironment environment, IOptions<StorageOptions> options)
    {
        this.environment = environment;
        this.options = options.Value;
    }

    public async Task<StoredImage> SaveAsync(ImageCategory category, Guid ownerId, IFormFile file, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file.Length == 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var (physicalPath, relativePath) = BuildPaths(category, ownerId, safeName);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        await using var stream = File.Create(physicalPath);
        await file.CopyToAsync(stream, ct);

        return new StoredImage(safeName, relativePath);
    }

    public Task DeleteAsync(ImageCategory category, Guid ownerId, string fileName, CancellationToken ct)
    {
        var (physicalPath, _) = BuildPaths(category, ownerId, fileName);
        DeleteFile(physicalPath);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string? relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var trimmed = relativePath.TrimStart('/', '\\');
        var physical = Path.Combine(GetUploadRoot(), trimmed.Replace('/', Path.DirectorySeparatorChar));
        DeleteFile(physical);
        return Task.CompletedTask;
    }

    public string GetUrl(HttpRequest request, ImageCategory category, Guid ownerId, string fileName)
    {
        var (_, relativePath) = BuildPaths(category, ownerId, fileName);
        return ToAbsolute(request, relativePath);
    }

    public string? GetUrl(HttpRequest request, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return ToAbsolute(request, normalized);
    }

    public string GetRelativePath(ImageCategory category, Guid ownerId, string fileName)
        => BuildPaths(category, ownerId, fileName).RelativePath;

    private (string PhysicalPath, string RelativePath) BuildPaths(ImageCategory category, Guid ownerId, string fileName)
    {
        var folder = category switch
        {
            ImageCategory.Area => "areas",
            ImageCategory.City => "cities",
            ImageCategory.House => "houses",
            ImageCategory.Feature => "features",
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
        };

        var sanitizedFile = Path.GetFileName(fileName);
        var segments = new List<string>();
        if (!string.IsNullOrWhiteSpace(options.UploadsPath))
        {
            segments.Add(options.UploadsPath.Trim('/', '\\'));
        }

        segments.Add(folder);
        segments.Add(ownerId.ToString());
        segments.Add(sanitizedFile);

        var relative = "/" + string.Join('/', segments);
        var physical = Path.Combine(GetUploadRoot(), Path.Combine(segments.ToArray()));
        return (physical, relative);
    }

    private string GetUploadRoot()
    {
        if (!string.IsNullOrWhiteSpace(environment.WebRootPath))
        {
            return environment.WebRootPath;
        }

        var fallback = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static void DeleteFile(string physicalPath)
    {
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private static string ToAbsolute(HttpRequest request, string relativePath)
    {
        var basePath = string.IsNullOrEmpty(request.PathBase) ? string.Empty : request.PathBase.Value!.TrimEnd('/');
        return $"{request.Scheme}://{request.Host}{basePath}{relativePath}";
    }
}
