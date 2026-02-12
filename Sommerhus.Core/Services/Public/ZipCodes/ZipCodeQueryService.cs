using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Services.Public.ZipCodes;

namespace Sommerhus.Core.Services.Public.ZipCodes;

public sealed class ZipCodeQueryService(AppDbContext db) : IZipCodeQueryService
{

    public async Task<IEnumerable<string>> FindAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<string>();
        }

        var term = query.Trim();
        return await db.Cities.AsNoTracking()
            .Where(c => c.Zip != null && EF.Functions.Like(c.Zip, $"{term}%"))
            .OrderBy(c => c.Zip)
            .Select(c => c.Zip!)
            .Distinct()
            .Take(20)
            .ToListAsync(ct);
    }
}
