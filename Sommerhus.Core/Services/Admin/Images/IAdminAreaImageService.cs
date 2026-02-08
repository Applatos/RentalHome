using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Images;

public interface IAdminAreaImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid areaId, string baseUrl, CancellationToken ct);
    Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, string baseUrl, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid areaId, IFormFileCollection files, string baseUrl, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid areaId, Guid imageId, CancellationToken ct);
}
