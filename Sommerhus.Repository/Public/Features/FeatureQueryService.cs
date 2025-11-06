using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Application.Public.Features;
using Sommerhus.Application.Storage;
using Sommerhus.Contracts.Dtos.Admin.Features;

namespace Sommerhus.Repository.Public.Features;

public sealed class FeatureQueryService : IFeatureQueryService
{
    private readonly AppDbContext db;
    private readonly IImageStorage storage;

    public FeatureQueryService(AppDbContext db, IImageStorage storage)
    {
        this.db = db;
        this.storage = storage;
    }

    public async Task<IEnumerable<FeatureDetailsDto>> GetAllAsync(HttpRequest request, CancellationToken ct)
    {
        var rows = await db.Features.AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        return rows.Select(f =>
        {
            var icon = storage.GetUrl(request, f.IconUrl);
            return new FeatureDetailsDto(f.Id, f.Name, f.Key, f.ValueType.ToString(), f.Unit, icon);
        }).ToList();
    }
}
