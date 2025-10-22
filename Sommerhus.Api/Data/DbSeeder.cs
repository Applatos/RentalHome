using Sommerhus.Api.Models;
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
            Id = Guid.NewGuid(),
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

