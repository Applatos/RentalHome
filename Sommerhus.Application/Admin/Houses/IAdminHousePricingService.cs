using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;

namespace Sommerhus.Application.Admin.Houses;

public interface IAdminHousePricingService
{
    Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct);
}
