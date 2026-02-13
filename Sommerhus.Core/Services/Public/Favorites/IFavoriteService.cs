using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Favorites;

public interface IFavoriteService
{
    Task<IReadOnlyList<FavoriteHouseDto>> ListAsync(string userId, CancellationToken ct);
    Task<ServiceResult> AddAsync(string userId, Guid houseId, CancellationToken ct);
    Task<ServiceResult> RemoveAsync(string userId, Guid houseId, CancellationToken ct);
    Task<bool> IsFavoritedAsync(string userId, Guid houseId, CancellationToken ct);
    Task<IReadOnlySet<Guid>> GetFavoriteIdsAsync(string userId, IEnumerable<Guid> houseIds, CancellationToken ct);
}
