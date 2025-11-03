using Microsoft.AspNetCore.Http;
using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Houses;

public interface IAdminHouseService
{
    Task<PageResult<HouseListItemDto>> SearchAsync(string? query, int page, int pageSize, CancellationToken ct);
    Task<ServiceResult<HouseDetailsDto>> GetDetailsAsync(Guid id, HttpRequest request, CancellationToken ct);
    Task<ServiceResult<Guid>> CreateAsync(UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> UpdateAsync(Guid id, UpsertHouseDto dto, CancellationToken ct);
    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken ct);
    Task<FeatureUpsertOutcome> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct);
    Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct);
}
