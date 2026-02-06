using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Features;

public interface IFeatureQueryService
{
    Task<IEnumerable<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct);
}
