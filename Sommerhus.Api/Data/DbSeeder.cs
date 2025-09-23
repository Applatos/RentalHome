using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Data;

public static class Seeder
{
    public static void SeedMinimal(AppDbContext db)
    {
        if (db.Cities.Any()) return;

        // --- Cities ---
        var city = new City
        {
            Name = "Blåvand",
            Zip = "6857",
            Slug = "blavand",
            Description = "Blåvand byder på bred sandstrand og naturoplevelser."
        };
        db.Cities.Add(city);

        // --- Areas ---
        var area = new Area
        {
            Name = "Blåvand Strand",
            Description = "Kendt for bred sandstrand og fyrtårn.",
            City = city
        };
        db.Areas.Add(area);

        // --- Features ---
        var f1 = new Feature { Name = "Pool", Key = "pool", ValueType = FeatureValueType.Bool, IconUrl = "/icons/pool.svg" };
        var f2 = new Feature { Name = "Antal senge", Key = "beds", ValueType = FeatureValueType.Int, Unit = "senge", IconUrl = "/icons/bed.svg" };
        var f3 = new Feature { Name = "Afstand til strand", Key = "beach_distance", ValueType = FeatureValueType.Int, Unit = "m", IconUrl = "/icons/beach.svg" };
        db.Features.AddRange(f1, f2, f3);

        // --- Houses ---
        var house = new VacationHouse
        {
            Title = "Hyggeligt sommerhus",
            Subtitle = "Perfekt til familieferie",
            Address = "Strandvej 10",
            City = city,
            Description = "Lyst og rummeligt hus nær stranden.",
            Facilities = "WiFi, Grill, Terrasse",
            CreatedUtc = DateTime.UtcNow
        };
        db.Houses.Add(house);

        // House images
        db.Images.Add(new HouseImage { House = house, FileName = "cover1.jpg", Kind = ImageKind.Cover, Alt = "Forsidebillede" });
        db.Images.Add(new HouseImage { House = house, FileName = "gallery1.jpg", Kind = ImageKind.Gallery });

        // House features
        db.HouseFeatureValues.Add(new HouseFeatureValue { House = house, Feature = f1, RawValue = "true" });
        db.HouseFeatureValues.Add(new HouseFeatureValue { House = house, Feature = f2, RawValue = "6" });
        db.HouseFeatureValues.Add(new HouseFeatureValue { House = house, Feature = f3, RawValue = "300" });

        // Area images
        db.AreaImages.Add(new AreaImage { Area = area, FileName = "uploads/areas/blavand_strand1.jpg", SortOrder = 1 });

        db.CityImages.Add(new CityImage { City = city, FileName = "city_blavand.jpg", SortOrder = 0, Alt = "Blåvand" });

        db.SaveChanges();
    }
}










//var desired = new (string Name, string Key, FeatureValueType Type, string? Unit, int Sort)[]
//{
//    ("Afstand til strand", "beach",      FeatureValueType.Int,     "m",   10),
//    ("Grundareal",         "groundsize", FeatureValueType.Int,     "m²",  20),
//    ("Boligareal",         "housesize",  FeatureValueType.Int,     "m²",  30),
//    ("Panorama",           "panorama",   FeatureValueType.Bool,    null,  40),
//    ("Personer",           "personer",   FeatureValueType.Int,     "pers",50),
//    ("Rengøring",          "rengøring",  FeatureValueType.Bool,    null,  60),
//    ("Sauna",              "sauna",      FeatureValueType.Bool,    null,  70),
//    ("Indkøb",             "shopping",   FeatureValueType.Bool,    null,  80),
//    ("Pool / Spa",         "spa",        FeatureValueType.Bool,    null,  90),
//    ("TV",                 "tv",         FeatureValueType.Bool,    null, 100),
//    ("Wi-Fi",              "wifi",       FeatureValueType.Bool,    null, 110),
//};