namespace Sommerhus.Core.Services.Public.Pricing;

public interface IPriceSummaryService
{
    Task<Dictionary<Guid, (decimal? Min, decimal? Max, string? Currency)>> GetSummariesAsync(IEnumerable<Guid> houseIds, CancellationToken ct);

    /// <summary>
    /// Propagates a price-affecting change to the given houses: deletes their cached quotes,
    /// recomputes their from-price summary and refreshes their search documents. Every change to a
    /// house's plan, group, calendar override or effective calendar must end here.
    /// </summary>
    Task RecomputeSummariesAsync(IEnumerable<Guid> houseIds, CancellationToken ct);

    /// <summary>
    /// <see cref="RecomputeSummariesAsync"/> for one house.
    /// </summary>
    Task RecomputeSummaryAsync(Guid houseId, CancellationToken ct);

    /// <summary>
    /// <see cref="RecomputeSummariesAsync"/> for every house whose effective calendar is
    /// <paramref name="calendarId"/>: houses with it as override, and houses without an override
    /// whose group uses it.
    /// </summary>
    Task RecomputeSummariesForCalendarAsync(Guid calendarId, CancellationToken ct);
}
