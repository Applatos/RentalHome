using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Features;

public interface IAdminFeatureService
{
    Task<IReadOnlyList<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct);
    Task<ServiceResult<Guid>> CreateAsync(UpsertFeatureDto dto, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertFeatureDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<string>> UploadIconAsync(Guid id, IFormFile file, string baseUrl, CancellationToken ct);
    Task<ServiceResult> DeleteIconAsync(Guid id, CancellationToken ct);
}
