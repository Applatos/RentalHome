namespace Sommerhus.Core.Services.Public.Houses;

public interface ISearchIndexer
{
    Task RebuildAsync(CancellationToken ct);
    Task UpdateHouseAsync(Guid houseId, CancellationToken ct);
    Task RemoveHouseAsync(Guid houseId, CancellationToken ct);
}
