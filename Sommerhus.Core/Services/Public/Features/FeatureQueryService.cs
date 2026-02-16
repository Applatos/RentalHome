using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Public.Features;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Features;

public sealed class FeatureQueryService(AppDbContext db, IImageStorage storage) : IFeatureQueryService
{

    public async Task<IEnumerable<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var icon = storage.GetUrl(baseUrl, f.IconUrl);
            return new FeatureDto(
                f.Id,
                LocalizationNameResolver.Resolve(f.Name, f.NameEn),
                f.Key,
                f.ValueType,
                f.Category,
                f.IsSearchable,
                f.Options,
                f.Unit,
                icon);
        }).ToList();
    }

    public async Task<Dictionary<string, IReadOnlyList<SearchableFeatureDto>>> GetSearchableAsync(CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .Where(f => f.IsSearchable)
            .OrderBy(f => f.Category)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .ToListAsync(ct);

        return rows
            .GroupBy(f => f.Category.ToString())
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<SearchableFeatureDto>)g
                    .Select(f => new SearchableFeatureDto(
                        f.Key,
                        LocalizationNameResolver.Resolve(f.Name, f.NameEn),
                        f.ValueType,
                        f.Unit,
                        f.Options))
                    .ToList());
    }
}
