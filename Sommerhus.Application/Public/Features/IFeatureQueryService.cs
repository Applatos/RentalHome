using Microsoft.AspNetCore.Http;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Application.Public.Features;

public interface IFeatureQueryService
{
    Task<IEnumerable<FeatureDetailsDto>> GetAllAsync(HttpRequest request, CancellationToken ct);
}
