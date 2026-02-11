using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Core.Services.Owner;

public sealed class OwnerAuthorizationService(AppDbContext db) : IOwnerAuthorizationService
{
    private readonly AppDbContext db = db;

    public async Task<bool> IsOwnerAsync(string userId, Guid houseId, CancellationToken ct)
        => await db.Houses.AsNoTracking()
            .AnyAsync(h => h.Id == houseId && h.OwnerId == userId, ct);
}
