using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Images;

/// <summary>
/// Generic interface for entity image management operations.
/// Provides common CRUD operations for images attached to any entity type.
/// </summary>
/// <typeparam name="TEntity">The entity type that owns the images</typeparam>
public interface IAdminEntityImageService<TEntity>
{
    /// <summary>
    /// Lists all images for the specified entity.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid entityId, string baseUrl, CancellationToken ct);

    /// <summary>
    /// Deletes an image from the specified entity.
    /// </summary>
    Task<ServiceResult> DeleteAsync(Guid entityId, Guid imageId, CancellationToken ct);
}

/// <summary>
/// Extended interface for entities that support single file uploads.
/// </summary>
public interface IAdminSingleImageUploadService<TEntity> : IAdminEntityImageService<TEntity>
{
    /// <summary>
    /// Uploads a single image to the specified entity.
    /// </summary>
    Task<ServiceResult<ImageDto>> UploadAsync(Guid entityId, IFormFile file, string baseUrl, CancellationToken ct);
}

/// <summary>
/// Extended interface for entities that support batch file uploads.
/// </summary>
public interface IAdminBatchImageUploadService<TEntity> : IAdminEntityImageService<TEntity>
{
    /// <summary>
    /// Uploads multiple images to the specified entity.
    /// </summary>
    Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid entityId, IFormFileCollection files, string baseUrl, CancellationToken ct);
}
