using Sommerhus.Application.Common;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Pricing;

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
