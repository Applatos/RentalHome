using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Data;

public static class ReferenceDataSeeder
{
    public static async Task<int> SeedCitiesAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existingZips = (await db.Cities.AsNoTracking().Select(city => city.Zip).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = 0;

        // Postal districts are required reference data in every environment. Match by ZIP,
        // preserving existing IDs, names, descriptions and relationships on each restart.
        foreach (var district in DanishGeoData.Load())
        {
            if (!existingZips.Add(district.Nr))
                continue;

            db.Cities.Add(new City { Zip = district.Nr, Name = district.Navn });
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(ct);

        return added;
    }
}
