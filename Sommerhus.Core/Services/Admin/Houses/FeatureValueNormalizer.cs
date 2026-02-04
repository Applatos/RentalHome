using Sommerhus.Core.Dtos.Admin.Features;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Houses;

public static class FeatureValueNormalizer
{
    public static List<HouseFeatureValue> Normalize(Guid houseId, IEnumerable<PostFeatureValueDto>? values)
    {
        if (values is null)
        {
            return new List<HouseFeatureValue>();
        }

        return values
            .Where(v => v is not null)
            .Select(v => new
            {
                v.FeatureId,
                Raw = (v.RawValue ?? string.Empty).Trim()
            })
            .Where(x => x.FeatureId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Raw))
            .GroupBy(x => x.FeatureId)
            .Select(g => new HouseFeatureValue
            {
                HouseId = houseId,
                FeatureId = g.Key,
                RawValue = g.First().Raw
            })
            .ToList();
    }
}
