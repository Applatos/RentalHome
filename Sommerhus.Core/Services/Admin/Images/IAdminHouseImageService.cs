using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Images;

public interface IAdminHouseImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid houseId, string baseUrl, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid houseId, IFormFileCollection files, string baseUrl, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid houseId, Guid imageId, CancellationToken ct);
    Task<ServiceResult> SetKindAsync(Guid houseId, Guid imageId, ImageKind kind, CancellationToken ct);
}
