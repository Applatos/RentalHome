using Sommerhus.Api.Models;

namespace Sommerhus.Api.Data;

public static class Seeder
{
    public static void SeedMinimal(AppDbContext db)
    {
        if (db.Features.Any()) return;

        var boolF = new Feature { Id = Guid.NewGuid(), Name = "Sauna", Key = "sauna", ValueType = FeatureValueType.Bool, SortOrder = 10 };
        var sizeF = new Feature { Id = Guid.NewGuid(), Name = "Areal", Key = "areal", ValueType = FeatureValueType.Int, Unit = "m2", SortOrder = 20 };

        var city = new City { Id = Guid.NewGuid(), Name = "Blåvand", Zip = "6857" };
        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Blåvand",
            Description = "Hyggeligt område.",
            Cities = new List<City> { city }
        };

        var house = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = "Blåvand Strand 4",
            City = city,
            Address = "Strandvej 4",
            Description = "Super dejligt poolhus ...",
            Facilities = "Trådløst internet, Brændeovn ...",
            Areas = new List<Area> { area }
        };

        db.Features.AddRange(boolF, sizeF);
        db.Cities.Add(city);
        db.Areas.Add(area);
        db.Houses.Add(house);

        db.HouseFeatures.AddRange(
            new HouseFeatureValue { House = house, Feature = boolF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = sizeF, RawValue = "210" }
        );

        db.SaveChanges();
    }
}
