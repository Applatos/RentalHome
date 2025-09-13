using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // --- Kun disse features må findes (nøgler matcher /images/features/{key}.png i MVC) ---
        var desired = new (string Name, string Key, FeatureValueType Type, string? Unit, int Sort)[]
        {
            ("Afstand til strand", "beach",      FeatureValueType.Int,     "m",   10),
            ("Grundareal",         "groundsize", FeatureValueType.Int,     "m²",  20),
            ("Boligareal",         "housesize",  FeatureValueType.Int,     "m²",  30),
            ("Panorama",           "panorama",   FeatureValueType.Bool,    null,  40),
            ("Personer",           "personer",   FeatureValueType.Int,     "pers",50),
            ("Rengøring",          "rengøring",  FeatureValueType.Bool,    null,  60),
            ("Sauna",              "sauna",      FeatureValueType.Bool,    null,  70),
            ("Indkøb",             "shopping",   FeatureValueType.Bool,    null,  80),
            ("Pool / Spa",         "spa",        FeatureValueType.Bool,    null,  90),
            ("TV",                 "tv",         FeatureValueType.Bool,    null, 100),
            ("Wi-Fi",              "wifi",       FeatureValueType.Bool,    null, 110),
        };

        var keep = desired.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);

        // 1) Slet alt der ikke er i 'desired'
        var toRemove = await db.Features.Where(f => !keep.Contains(f.Key)).Select(f => f.Id).ToListAsync();
        if (toRemove.Count > 0)
        {
            var removeValues = db.HouseFeatureValues.Where(v => toRemove.Contains(v.FeatureId));
            db.HouseFeatureValues.RemoveRange(removeValues);

            var removeFeatures = db.Features.Where(f => toRemove.Contains(f.Id));
            db.Features.RemoveRange(removeFeatures);
            await db.SaveChangesAsync();
        }

        // 2) Upsert 'desired'
        foreach (var d in desired)
        {
            var f = await db.Features.FirstOrDefaultAsync(x => x.Key == d.Key);
            if (f is null)
            {
                f = new Feature { Name = d.Name, Key = d.Key, ValueType = d.Type, Unit = d.Unit, SortOrder = d.Sort };
                db.Features.Add(f);
            }
            else
            {
                f.Name = d.Name;
                f.ValueType = d.Type;
                f.Unit = d.Unit;
                f.SortOrder = d.Sort;
            }
        }

        await db.SaveChangesAsync();
    }
    public static void Seed_cities(AppDbContext db)
    {
        if (!db.Cities.Any())
        {
            db.Cities.AddRange(
                new City { Name = "Ho", Zip = "6857", Slug = "ho", Text = "Hyggeligt sommerhusområde tæt på Blåvand." },
                new City { Name = "Blåvand", Zip = "6857", Slug = "blaavand", Text = "Kendt for sin strand og sit fyr." }
            );
            db.SaveChanges();
        }
    }
}
