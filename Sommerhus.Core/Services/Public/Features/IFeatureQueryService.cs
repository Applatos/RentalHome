using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Public.Features;

public interface IFeatureQueryService
{
    Task<IEnumerable<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct);
    Task<Dictionary<string, IReadOnlyList<SearchableFeatureDto>>> GetSearchableAsync(CancellationToken ct);
}
