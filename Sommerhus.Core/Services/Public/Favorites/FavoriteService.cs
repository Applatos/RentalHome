using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Favorites;

public sealed class FavoriteService(AppDbContext db) : IFavoriteService
{
    public async Task<IReadOnlyList<FavoriteHouseDto>> ListAsync(string userId, CancellationToken ct)
    {
        return await db.FavoriteHouses
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.House)
                .ThenInclude(h => h.City)
            .Include(f => f.House)
                .ThenInclude(h => h.Images)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Select(f => new FavoriteHouseDto(
                f.HouseId,
                f.House.Title,
                f.House.Images
                    .Where(i => i.Kind == ImageKind.Cover)
                    .Select(i => i.FileName)
                    .FirstOrDefault(),
                f.House.City != null ? f.House.City.Name : null,
                f.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ServiceResult> AddAsync(string userId, Guid houseId, CancellationToken ct)
    {
        var houseExists = await db.Houses.AnyAsync(h => h.Id == houseId && h.Status == EntityStatus.Published, ct);
        if (!houseExists)
            return ServiceResult.NotFound();

        var alreadyFavorited = await db.FavoriteHouses
            .AnyAsync(f => f.UserId == userId && f.HouseId == houseId, ct);

        if (alreadyFavorited)
            return ServiceResult.Success();

        db.FavoriteHouses.Add(new FavoriteHouse
        {
            UserId = userId,
            HouseId = houseId,
            CreatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RemoveAsync(string userId, Guid houseId, CancellationToken ct)
    {
        var favorite = await db.FavoriteHouses
            .FirstOrDefaultAsync(f => f.UserId == userId && f.HouseId == houseId, ct);

        if (favorite is null)
            return ServiceResult.Success();

        db.FavoriteHouses.Remove(favorite);
        await db.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<bool> IsFavoritedAsync(string userId, Guid houseId, CancellationToken ct) =>
        await db.FavoriteHouses.AnyAsync(f => f.UserId == userId && f.HouseId == houseId, ct);

    public async Task<IReadOnlySet<Guid>> GetFavoriteIdsAsync(string userId, IEnumerable<Guid> houseIds, CancellationToken ct)
    {
        var ids = houseIds.ToList();
        if (ids.Count == 0)
            return new HashSet<Guid>();

        var favorited = await db.FavoriteHouses
            .Where(f => f.UserId == userId && ids.Contains(f.HouseId))
            .Select(f => f.HouseId)
            .ToListAsync(ct);

        return favorited.ToHashSet();
    }
}
