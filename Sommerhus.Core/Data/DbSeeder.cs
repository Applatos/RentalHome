using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Data;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core;

public static class Seeder
{
    public static void SeedMinimal(AppDbContext db)
    {
        ReferenceDataSeeder.SeedCitiesAsync(db).GetAwaiter().GetResult();
        if (db.Features.Any()) return;

        // Property features
        var bedroomsF = new Feature { Id = Guid.NewGuid(), Name = "Bedrooms", Key = "bedrooms", ValueType = FeatureValueType.Int, Category = FeatureCategory.Property, Options = "1,2,3,4,5,6+", SortOrder = 10 };
        var bathroomsF = new Feature { Id = Guid.NewGuid(), Name = "Bathrooms", Key = "bathrooms", ValueType = FeatureValueType.Int, Category = FeatureCategory.Property, Options = "1,2,3,4+", SortOrder = 20 };
        var maxGuestsF = new Feature { Id = Guid.NewGuid(), Name = "Max guests", Key = "max_guests", ValueType = FeatureValueType.Int, Category = FeatureCategory.Property, Options = "1-2,3-4,5-6,7-8,9+", SortOrder = 30 };
        var sizeF = new Feature { Id = Guid.NewGuid(), Name = "Size (m²)", Key = "size_m2", ValueType = FeatureValueType.Int, Category = FeatureCategory.Property, Unit = "m²", SortOrder = 40 };

        // Facility features
        var poolF = new Feature { Id = Guid.NewGuid(), Name = "Swimming pool", Key = "pool", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 100 };
        var saunaF = new Feature { Id = Guid.NewGuid(), Name = "Sauna", Key = "sauna", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 110 };
        var spaF = new Feature { Id = Guid.NewGuid(), Name = "Spa / hot tub", Key = "spa", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 120 };
        var wifiF = new Feature { Id = Guid.NewGuid(), Name = "Internet / Wifi", Key = "wifi", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 130 };
        var dishwasherF = new Feature { Id = Guid.NewGuid(), Name = "Dishwasher", Key = "dishwasher", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 140 };
        var washingF = new Feature { Id = Guid.NewGuid(), Name = "Washing machine", Key = "washing_machine", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 150 };
        var dryerF = new Feature { Id = Guid.NewGuid(), Name = "Tumble dryer", Key = "dryer", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 160 };
        var petF = new Feature { Id = Guid.NewGuid(), Name = "Pet friendly", Key = "pet_friendly", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 170 };
        var fireplaceF = new Feature { Id = Guid.NewGuid(), Name = "Fireplace", Key = "fireplace", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 180 };
        var acF = new Feature { Id = Guid.NewGuid(), Name = "Air conditioning", Key = "aircondition", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 190 };
        var evF = new Feature { Id = Guid.NewGuid(), Name = "EV charger", Key = "ev_charger", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 200 };
        var nonsmokingF = new Feature { Id = Guid.NewGuid(), Name = "Non-smoking", Key = "nonsmoking", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 210 };
        var handicapF = new Feature { Id = Guid.NewGuid(), Name = "Handicap accessible", Key = "handicap", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Facility, SortOrder = 220 };

        // Distance features
        var distShopF = new Feature { Id = Guid.NewGuid(), Name = "Distance to shop", Key = "distance_shop", ValueType = FeatureValueType.Int, Category = FeatureCategory.Distance, Unit = "m", Options = "< 500m,< 1 km,< 2 km,< 5 km", SortOrder = 300 };
        var distWaterF = new Feature { Id = Guid.NewGuid(), Name = "Distance to water", Key = "distance_water", ValueType = FeatureValueType.Int, Category = FeatureCategory.Distance, Unit = "m", Options = "< 100m,< 500m,< 1 km,< 5 km", SortOrder = 310 };
        var waterViewF = new Feature { Id = Guid.NewGuid(), Name = "Water view", Key = "water_view", ValueType = FeatureValueType.Bool, Category = FeatureCategory.Distance, SortOrder = 320 };

        var allFeatures = new Feature[] { bedroomsF, bathroomsF, maxGuestsF, sizeF, poolF, saunaF, spaF, wifiF, dishwasherF, washingF, dryerF, petF, fireplaceF, acF, evF, nonsmokingF, handicapF, distShopF, distWaterF, waterViewF };


        var city = db.Cities.First(c => c.Zip == "6857");


        var groupA = new HouseGroup{ Id = Guid.NewGuid(), Name = "Vesterhavet"};
        var groupB = new HouseGroup{ Id = Guid.NewGuid(), Name = "Tyskland" };


        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Blåvand",
            Description = "Hyggeligt område.",
            Status = EntityStatus.Published,
            Cities = new List<City> { city }
        };

        var house = new VacationHouse
        {
            Id = new Guid("5fb7097c-335c-4d07-b4fd-000004e2d28c"),
            Title = "Blåvand Strand 4",
            City = city,
            Address = "Strandvej 4",
            Description = "Super dejligt poolhus ...",
            SearchKeywords = "pool, beach, family-friendly, modern",
            Status = EntityStatus.Published,
            PublishedAtUtc = DateTime.UtcNow,
            Areas = new List<Area> { area },
            Group = groupA
        };

        var seasonA = new SeasonCode { Code = "A", Color = "#FF5733", Name = "Højsæson" };
        var seasonB = new SeasonCode { Code = "B",  Color = "#33C1FF", Name = "Sommer" };



        db.SeasonCodes.AddRange(seasonA, seasonB);
        db.Features.AddRange(allFeatures);
        db.AddRange(groupA, groupB);
        db.Areas.Add(area);
        db.Houses.Add(house);

        db.HouseFeatures.AddRange(
            new HouseFeatureValue { House = house, Feature = bedroomsF, RawValue = "3" },
            new HouseFeatureValue { House = house, Feature = bathroomsF, RawValue = "2" },
            new HouseFeatureValue { House = house, Feature = maxGuestsF, RawValue = "6" },
            new HouseFeatureValue { House = house, Feature = sizeF, RawValue = "210" },
            new HouseFeatureValue { House = house, Feature = poolF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = saunaF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = wifiF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = dishwasherF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = petF, RawValue = "false" },
            new HouseFeatureValue { House = house, Feature = distShopF, RawValue = "800" },
            new HouseFeatureValue { House = house, Feature = distWaterF, RawValue = "200" }
        );

        // Create a SeasonCalendar for groupA and assign spans to it
        var groupACalendar = new SeasonCalendar
        {
            Id = Guid.NewGuid(),
            Name = "Vesterhavet Calendar",
            IsTemplate = false
        };
        groupA.DefaultCalendar = groupACalendar;

        var calendarSegments = new List<SeasonSpan>();
        var currentYear = DateTime.UtcNow.Year;
        var seededYears = new[] { currentYear, currentYear + 1 };

        foreach (var year in seededYears)
        {
            var winterEndDay = DateTime.IsLeapYear(year) ? 29 : 28;
            
            // Winter (Jan-Feb) - Season A (High season)
            calendarSegments.Add(new SeasonSpan
            {
                Id = Guid.NewGuid(),
                CalendarId = groupACalendar.Id,
                Code = "A",
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 2, winterEndDay)
            });
            
            // Spring (Mar-May) - Season B (Sommer)
            calendarSegments.Add(new SeasonSpan
            {
                Id = Guid.NewGuid(),
                CalendarId = groupACalendar.Id,
                Code = "B",
                StartDate = new DateOnly(year, 3, 1),
                EndDate = new DateOnly(year, 5, 31)
            });
            
            // Summer (Jun-Aug) - Season A (High season)
            calendarSegments.Add(new SeasonSpan
            {
                Id = Guid.NewGuid(),
                CalendarId = groupACalendar.Id,
                Code = "A",
                StartDate = new DateOnly(year, 6, 1),
                EndDate = new DateOnly(year, 8, 31)
            });
            
            // Fall (Sep-Dec) - Season B (Sommer)
            calendarSegments.Add(new SeasonSpan
            {
                Id = Guid.NewGuid(),
                CalendarId = groupACalendar.Id,
                Code = "B",
                StartDate = new DateOnly(year, 9, 1),
                EndDate = new DateOnly(year, 12, 31)
            });
        }

        db.SeasonCalendars.Add(groupACalendar);

        var seasonRates = new List<SeasonPrice>
        {
            new SeasonPrice { Code = "A", NightlyPrice = 800m },
            new SeasonPrice { Code = "B", NightlyPrice = 950m },
        };



        var plan = new PricePlan
        {
            HouseId = house.Id,
            Name = "Standard",
            Currency = "DKK",
            IsActive = true,
            SeasonPrices = seasonRates
        };

        db.PricePlans.Add(plan);
        db.SeasonSpans.AddRange(calendarSegments);
        db.HousePriceSummaries.Add(new HousePriceSummary
        {
            HouseId = house.Id,
            MinNightlyPrice = seasonRates.Min(r => r.NightlyPrice),
            MaxNightlyPrice = seasonRates.Max(r => r.NightlyPrice),
            Currency = plan.Currency,
            ComputedAtUtc = DateTime.UtcNow
        });

        db.Set<HouseSearchDocument>().Add(new HouseSearchDocument
        {
            HouseId = house.Id,
            Title = house.Title,
            Description = house.Description,
            CityName = city.Name,
            CityZip = city.Zip,
            Address = house.Address,
            AreaNames = area.Name,
            SearchKeywords = house.SearchKeywords,
            Status = EntityStatus.Published,
            MinNightlyPrice = seasonRates.Min(r => r.NightlyPrice),
            MaxNightlyPrice = seasonRates.Max(r => r.NightlyPrice),
            Currency = plan.Currency,
            Bedrooms = 3,
            MaxGuests = 6,
            HasPool = true,
            PetFriendly = false,
            SearchVector = $"{house.Title} {house.Description} {city.Name} {city.Zip} {house.Address} {area.Name} {house.SearchKeywords}",
            UpdatedAtUtc = DateTime.UtcNow
        });

        db.SaveChanges();
    }


}
