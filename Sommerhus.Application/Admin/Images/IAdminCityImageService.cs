using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Images;

public interface IAdminCityImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid cityId, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<ImageDto>> UploadAsync(Guid cityId, IFormFile file, string? alt, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid cityId, Guid imageId, CancellationToken ct);
}
