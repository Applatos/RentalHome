using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models;
using Sommerhus.Pricing.Models;
using System.Text.Json;

namespace Sommerhus.Repository;

public static class Seeder
{
    public static void SeedMinimal(AppDbContext db)
    {
        if (db.Features.Any()) return;

        var boolF = new Feature { Id = Guid.NewGuid(), Name = "Sauna", Key = "sauna", ValueType = FeatureValueType.Bool, SortOrder = 10 };
        var sizeF = new Feature { Id = Guid.NewGuid(), Name = "Areal", Key = "areal", ValueType = FeatureValueType.Int, Unit = "m2", SortOrder = 20 };


        var cities = LoadDanishCities();
        var city = cities.FirstOrDefault(c => c.Zip == "6857") ?? cities.First();


        var groupA = new HouseGroup{ Id = Guid.NewGuid(), Name = "Vesterhavet"};
        var groupB = new HouseGroup{ Id = Guid.NewGuid(), Name = "Tyskland" };


        var area = new Area
        {
            Id = Guid.NewGuid(),
            Name = "Blåvand",
            Description = "Hyggeligt område.",
            Cities = new List<City> { city }
        };

        var house = new VacationHouse
        {
            Id = new Guid("5fb7097c-335c-4d07-b4fd-000004e2d28c"),
            Title = "Blåvand Strand 4",
            City = city,
            Address = "Strandvej 4",
            Description = "Super dejligt poolhus ...",
            Facilities = "Trådløst internet, Brændeovn ...",
            Areas = new List<Area> { area },
            Group = groupA
        };

        var seasonA = new SeasonCode { Code = "A", Color = "#FF5733", Name = "Højsæson" };
        var seasonB = new SeasonCode { Code = "B",  Color = "#33C1FF", Name = "Sommer" };



        db.SeasonCodes.AddRange(seasonA, seasonB);
        db.Features.AddRange(boolF, sizeF);
        db.AddRange(groupA, groupB);
        db.Cities.AddRange(cities);
        db.Areas.Add(area);
        db.Houses.Add(house);

        db.HouseFeatures.AddRange(
            new HouseFeatureValue { House = house, Feature = boolF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = sizeF, RawValue = "210" }
        );

        var CalenderSegments = new List<SeasonSpan>();
        var currentYear = DateTime.UtcNow.Year;
        var seededYears = new[] { currentYear, currentYear + 1 };

        foreach (var year in seededYears)
        {
            var winterEndDay = DateTime.IsLeapYear(year) ? 29 : 28;
            CalenderSegments.Add( new SeasonSpan {
                GroupId = groupA.Id,
                Id = Guid.NewGuid(),
                Code = "A",
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 2, winterEndDay)
            });
            CalenderSegments.Add(new SeasonSpan
            {
                Id = Guid.NewGuid(),
                GroupId = groupB.Id,
                Code = "B",
                StartDate = new DateOnly(year, 3, 1),
                EndDate = new DateOnly(year, 5, 31)
            });
        };

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
            SeasonPrices = seasonRates
        };

        db.PricePlans.Add(plan);
        db.SeasonSpans.AddRange(CalenderSegments);

        db.SaveChanges();
    }


    private static List<City> LoadDanishCities()
    {
        var cities = TryFetchDanishCities();
        if (cities.Count > 0)
        {
            return cities;
        }

        return new List<City>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Blåvand",
                Zip = "6857"
            }
        };
    }


    private static List<City> TryFetchDanishCities()
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(20)
            };

            using var response = client.GetAsync("https://api.dataforsyningen.dk/postnumre").GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using var responseStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            var postNumbers = JsonSerializer.Deserialize<List<PostNumberDto>>(responseStream);
            if (postNumbers is null)
            {
                return new List<City>();
            }

            var deduplicated = new Dictionary<string, City>(StringComparer.OrdinalIgnoreCase);

            foreach (var postNumber in postNumbers) //Cleanup and deduplicate
            {
                if (string.IsNullOrWhiteSpace(postNumber?.Nr) || string.IsNullOrWhiteSpace(postNumber.Navn))
                {
                    continue;
                }

                var zip = postNumber.Nr.Trim();
                if (deduplicated.ContainsKey(zip))
                {
                    continue;
                }

                deduplicated[zip] = new City
                {
                    Id = Guid.NewGuid(),
                    Zip = zip,
                    Name = postNumber.Navn.Trim()
                };
            }

            return deduplicated.Values
                .OrderBy(city => city.Zip, StringComparer.Ordinal)
                .ThenBy(city => city.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return new List<City>();
        }
    }
    private sealed class PostNumberDto
    {
        public string? Nr { get; set; }
        public string? Navn { get; set; }
    }
}
