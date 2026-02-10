using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Availability;

public sealed class AvailabilityQueryService(AppDbContext db) : IAvailabilityQueryService
{
    public async Task<IReadOnlyList<AvailabilityBlockDto>?> GetBlocksAsync(
        Guid houseId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var houseExists = await db.Houses
            .AnyAsync(h => h.Id == houseId && h.Status == EntityStatus.Published, ct);

        if (!houseExists)
            return null;

        var blocks = await db.AvailabilityBlocks
            .AsNoTracking()
            .Where(b => b.HouseId == houseId
                        && b.StartDate < to
                        && b.EndDate > from
                        && b.Status != AvailabilityStatus.Available)
            .OrderBy(b => b.StartDate)
            .Select(b => new AvailabilityBlockDto(
                b.Id,
                b.HouseId,
                b.StartDate,
                b.EndDate,
                b.Status,
                b.Source,
                null,
                b.CreatedAtUtc,
                null))
            .ToListAsync(ct);

        return blocks;
    }
}
