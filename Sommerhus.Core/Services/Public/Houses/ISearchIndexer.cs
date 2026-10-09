namespace Sommerhus.Core.Services.Public.Houses;

public interface ISearchIndexer
{
    Task RebuildAsync(CancellationToken ct);
    Task UpdateHouseAsync(Guid houseId, CancellationToken ct);
    Task RemoveHouseAsync(Guid houseId, CancellationToken ct);

    /// <summary>Indexes every house that has no search document; returns how many it indexed.</summary>
    Task<int> IndexMissingAsync(CancellationToken ct);
}
