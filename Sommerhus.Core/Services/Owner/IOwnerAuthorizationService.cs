namespace Sommerhus.Core.Services.Owner;

public interface IOwnerAuthorizationService
{
    Task<bool> IsOwnerAsync(string userId, Guid houseId, CancellationToken ct);
}
