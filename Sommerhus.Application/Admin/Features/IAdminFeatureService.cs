using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Application.Admin.Features;

public interface IAdminFeatureService
{
    Task<IReadOnlyList<FeatureDetailsDto>> GetAllAsync(HttpRequest request, CancellationToken ct);
    Task<ServiceResult<Guid>> CreateAsync(UpsertFeatureDto dto, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertFeatureDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
    Task<ServiceResult<string>> UploadIconAsync(Guid id, IFormFile file, HttpRequest request, CancellationToken ct);
    Task<ServiceResult> DeleteIconAsync(Guid id, CancellationToken ct);
}
