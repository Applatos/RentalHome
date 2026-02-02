using Sommerhus.Contracts.Dtos.Shared;

namespace Sommerhus.Application.Admin.Houses;

public interface IAdminHouseFeatureService
{
    Task<FeatureUpsertOutcome> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct);
}
