using Microsoft.AspNetCore.Http;
using Sommerhus.Core.Dtos.Admin.Features;

namespace Sommerhus.Core.Services.Public.Features;

public interface IFeatureQueryService
{
    Task<IEnumerable<FeatureDetailsDto>> GetAllAsync(HttpRequest request, CancellationToken ct);
}
