using Sommerhus.Api.Models;
using Sommerhus.Pricing.Models;
using System.Text.Json;

namespace Sommerhus.Api.Data;

public static class Seeder
{
    public static void SeedMinimal(AppDbContext db)
    {
        if (db.Features.Any()) return;

        var boolF = new Feature { Id = Guid.NewGuid(), Name = "Sauna", Key = "sauna", ValueType = FeatureValueType.Bool, SortOrder = 10 };
        var sizeF = new Feature { Id = Guid.NewGuid(), Name = "Areal", Key = "areal", ValueType = FeatureValueType.Int, Unit = "m2", SortOrder = 20 };


        var cities = LoadDanishCities();
        var city = cities.FirstOrDefault(c => c.Zip == "6857") ?? cities.First();

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
            Areas = new List<Area> { area }
        };

        db.Features.AddRange(boolF, sizeF);
        db.Cities.AddRange(cities);
        db.Areas.Add(area);
        db.Houses.Add(house);

        db.HouseFeatures.AddRange(
            new HouseFeatureValue { House = house, Feature = boolF, RawValue = "true" },
            new HouseFeatureValue { House = house, Feature = sizeF, RawValue = "210" }
        );


        var seasons = new List<RateSeason>();
        var currentYear = DateTime.UtcNow.Year;
        foreach (var year in new[] { currentYear, currentYear + 1 })
        {
            var winterEndDay = DateTime.IsLeapYear(year) ? 29 : 28;
            seasons.Add(new RateSeason
            {
                Name = $"Vinter {year}",
                StartDate = new DateOnly(year, 1, 1),
                EndDate = new DateOnly(year, 2, winterEndDay),
                NightlyPrice = 800m
            });

            seasons.Add(new RateSeason
            {
                Name = $"Forår {year}",
                StartDate = new DateOnly(year, 3, 1),
                EndDate = new DateOnly(year, 5, 31),
                NightlyPrice = 950m
            });

            seasons.Add(new RateSeason
            {
                Name = $"Sommer {year}",
                StartDate = new DateOnly(year, 6, 1),
                EndDate = new DateOnly(year, 8, 31),
                NightlyPrice = 1400m,
                MinStayNights = 3
            });

            seasons.Add(new RateSeason
            {
                Name = $"Efterår {year}",
                StartDate = new DateOnly(year, 9, 1),
                EndDate = new DateOnly(year, 12, 31),
                NightlyPrice = 850m
            });
        }

        var plan = new RatePlan
        {
            HouseId = house.Id,
            Name = "Standard",
            Currency = "DKK",
            Seasons = seasons,
            Modifiers =
            {
                new RateModifier
                {
                    Name = "Weekend-tillæg",
                    Scope = PriceScope.PerNight,
                    Kind = AdjustmentKind.Absolute,
                    Value = 200m,
                    Trigger = ModifierTrigger.Weekend
                },
                new RateModifier
                {
                    Name = "Langtidsrabat",
                    Scope = PriceScope.PerBooking,
                    Kind = AdjustmentKind.Percent,
                    Value = -0.1m,
                    Trigger = ModifierTrigger.MinNights,
                    ThresholdNights = 7
                }
            }
        };

        db.RatePlans.Add(plan);

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
            var postNumbers = JsonSerializer.Deserialize<List<PostNumberDto>>(responseStream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

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

