using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sommerhus.Core.Services.Public.Pricing;

/// <summary>
/// Keeps from-prices current off the request path. A from-price depends on today's date (a season
/// counts only while its calendar has spans ahead), and rows written by an older rule need repair,
/// so every house is recomputed shortly after start and then every night.
/// </summary>
public sealed class PriceSummaryRefreshService(
    IServiceScopeFactory scopeFactory,
    ILogger<PriceSummaryRefreshService> logger) : BackgroundService
{
    // Just after midnight UTC, when "today" has moved on for every span check.
    private static readonly TimeSpan RunAfterMidnight = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Leave StartAsync at once so the host keeps starting.
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshAllAsync(stoppingToken);

            var now = DateTime.UtcNow;
            var next = now.Date.AddDays(1).Add(RunAfterMidnight);
            try
            {
                await Task.Delay(next - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Recomputes every house's from-price, one house per scope, so a failing house is skipped
    /// without stopping the rest and no DbContext grows with the number of houses.
    /// </summary>
    public async Task RefreshAllAsync(CancellationToken ct)
    {
        List<Guid> houseIds;
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            houseIds = await db.Houses.AsNoTracking().Select(h => h.Id).ToListAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Could not list houses for the from-price refresh");
            return;
        }

        var failed = 0;
        foreach (var houseId in houseIds)
        {
            ct.ThrowIfCancellationRequested();
            using var scope = scopeFactory.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<IPriceSummaryService>().RecomputeSummaryAsync(houseId, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failed++;
                logger.LogWarning(ex, "Could not refresh the from-price of house {HouseId}", houseId);
            }
        }

        logger.LogInformation("Refreshed from-prices for {Count} houses, {Failed} failed", houseIds.Count - failed, failed);
    }
}
