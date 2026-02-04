using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHouseFeatureService
{
    Task<FeatureUpsertOutcome> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct);
}
