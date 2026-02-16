namespace Sommerhus.Core.Services.Public.Pricing;

public interface IPriceSummaryService
{
    Task<Dictionary<Guid, (decimal? Min, decimal? Max, string? Currency)>> GetSummariesAsync(IEnumerable<Guid> houseIds, CancellationToken ct);
    Task RecomputeSummaryAsync(Guid houseId, CancellationToken ct);
    Task RecomputeSummariesForGroupAsync(Guid groupId, CancellationToken ct);
}
