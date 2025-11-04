using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Application.Admin.Images;

public interface IAdminHouseImageService
{
    Task<ServiceResult<IReadOnlyList<ImageDto>>> ListAsync(Guid houseId, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<ImageDto>>> UploadAsync(Guid houseId, IFormFileCollection files, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid houseId, Guid imageId, CancellationToken ct);
    Task<ServiceResult> SetKindAsync(Guid houseId, Guid imageId, ImageKind kind, CancellationToken ct);
}
