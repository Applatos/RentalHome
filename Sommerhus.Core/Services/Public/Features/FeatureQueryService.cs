using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.Features;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Core.Dtos.Shared;

namespace Sommerhus.Core.Services.Public.Features;

public sealed class FeatureQueryService : IFeatureQueryService
{
    private readonly AppDbContext db;
    private readonly IImageStorage storage;

    public FeatureQueryService(AppDbContext db, IImageStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    public async Task<IEnumerable<FeatureDto>> GetAllAsync(string baseUrl, CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var icon = storage.GetUrl(baseUrl, f.IconUrl);
            return new FeatureDto(f.Id, f.Name, f.Key, f.ValueType, f.Unit, icon);
        }).ToList();
    }
}
