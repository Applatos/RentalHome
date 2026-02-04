using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin.Pricing;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHousePricingService
{
    Task<ServiceResult<PricePlanDetailsDto>> UpsertPricingAsync(Guid houseId, PricePlanDetailsDto dto, CancellationToken ct);
}
