using Sommerhus.Core.Common;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Lifecycle;

public interface IEntityLifecycleService
{
    Task<ServiceResult> TransitionHouseAsync(Guid houseId, EntityStatus target, CancellationToken ct);
    Task<ServiceResult> TransitionAreaAsync(Guid areaId, EntityStatus target, CancellationToken ct);
}
