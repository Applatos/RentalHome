using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Admin.Houses;

public interface IAdminHouseFeatureService
{
    Task<ServiceResult> UpsertFeaturesAsync(Guid houseId, IEnumerable<PostFeatureValueDto>? values, CancellationToken ct);
}
