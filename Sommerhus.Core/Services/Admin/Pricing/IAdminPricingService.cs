using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared; using Sommerhus.Core.Dtos.Admin; using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.Pricing;

public interface IAdminPricingService
{
    Task<ServiceResult<PriceQuoteResponseDto>> QuoteAsync(PriceQuoteRequestDto request, CancellationToken ct);
    Task<IReadOnlyList<SeasonSpanDto>> GetSeasonSpansAsync(Guid groupId, CancellationToken ct);
    Task<ServiceResult<IReadOnlyList<SeasonSpanDto>>> UpsertSeasonSpansAsync(Guid groupId, IReadOnlyList<SeasonSpanDto> spans, CancellationToken ct);
    Task<IReadOnlyList<PricePlanDetailsDto>> GetRatePlansAsync(Guid houseId, CancellationToken ct);
    Task<ServiceResult> ActivateRatePlanAsync(Guid planId, CancellationToken ct);
    Task<ServiceResult> DeleteRatePlanAsync(Guid houseId, Guid ratePlanId, CancellationToken ct);
    Task<IReadOnlyList<SeasonCodeDto>> ListSeasonCodesAsync(CancellationToken ct);
    Task<ServiceResult<SeasonCodeDto>> CreateSeasonCodeAsync(SeasonCodeDto dto, CancellationToken ct);
}
