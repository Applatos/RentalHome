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
        var physical = Path.Combine(GetWebRoot(environment), trimmed.Replace('/', Path.DirectorySeparatorChar));
        DeleteFile(physical);
        return Task.CompletedTask;
    }

    public string GetUrl(string baseUrl, ImageCategory category, Guid ownerId, string fileName)
    {
        var (_, relativePath) = BuildPaths(category, ownerId, fileName);
        return ToAbsolute(baseUrl, relativePath);
    }

    public string? GetUrl(string baseUrl, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return ToAbsolute(baseUrl, normalized);
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
        var physical = Path.Combine(GetWebRoot(environment), Path.Combine(segments.ToArray()));
        return (physical, relative);
    }

    internal static string GetWebRoot(IWebHostEnvironment environment)
        => !string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? environment.WebRootPath
            : Path.Combine(environment.ContentRootPath, "wwwroot");

    private static void DeleteFile(string physicalPath)
    {
        if (System.IO.File.Exists(physicalPath))
        {
            System.IO.File.Delete(physicalPath);
        }
    }

    private static string ToAbsolute(string baseUrl, string relativePath)
        => $"{baseUrl.TrimEnd('/')}{relativePath}";
}
