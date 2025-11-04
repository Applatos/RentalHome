using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Images;

public interface IAdminAreaImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid areaId, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<ImageDto>> UploadAsync(Guid areaId, IFormFile file, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid areaId, Guid imageId, CancellationToken ct);
}
