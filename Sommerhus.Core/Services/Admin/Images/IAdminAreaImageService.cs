using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Images;

public interface IAdminAreaImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid areaId, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid areaId, Guid imageId, CancellationToken ct);
}
