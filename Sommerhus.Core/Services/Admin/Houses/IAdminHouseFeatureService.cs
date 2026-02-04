using Sommerhus.Core.Dtos.Shared; using Sommerhus.Core.Dtos.Admin; using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHouseFeatureService
{
    Task<FeatureUpsertOutcome> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct);
}
