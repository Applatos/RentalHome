using System.Reflection;
using System.Text.Json;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Core.Data;

public sealed class StressDataOptions
{
    public int HouseCount { get; set; } = 500;
    public int AreaCount { get; set; } = 50;
    public int CalendarCount { get; set; } = 10;
}

public interface IStressDataGenerator
{
    Task<StressDataResult> SeedAsync(StressDataOptions options, CancellationToken ct);
    Task<StressDataResult> ClearAsync(CancellationToken ct);
}

public sealed record StressDataResult(bool Success, string Message, int EntitiesAffected = 0);

public sealed class StressDataGenerator(AppDbContext db, ILogger<StressDataGenerator> logger) : IStressDataGenerator
{
    private const string StressTag = "__stress__";

    public async Task<StressDataResult> SeedAsync(StressDataOptions options, CancellationToken ct)
    {
        if (await db.Houses.AnyAsync(h => h.CreatedBy == StressTag, ct))
            return new StressDataResult(false, "Stress data already exists. Clear it first.");

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var rng = new Random(42);
        var totalEntities = 0;

        // 1. Load bundled Danish geo data
        var geoEntries = LoadBundledGeoData();
        logger.LogInformation("Loaded {Count} bundled Danish zip codes", geoEntries.Count);

        // 2. Ensure cities exist (reuse existing or create)
        var existingCities = await db.Cities.ToDictionaryAsync(c => c.Zip, ct);
        var newCities = new List<City>();
        foreach (var geo in geoEntries)
        {
            if (!existingCities.ContainsKey(geo.Nr))
            {
                var city = new City
                {
                    Id = Guid.NewGuid(),
                    Zip = geo.Nr,
                    Name = geo.Navn,
                    CreatedBy = StressTag
                };
                newCities.Add(city);
                existingCities[geo.Nr] = city;
            }
        }
        if (newCities.Count > 0)
        {
            db.Cities.AddRange(newCities);
            await db.SaveChangesAsync(ct);
            totalEntities += newCities.Count;
            logger.LogInformation("Created {Count} new cities", newCities.Count);
        }

        var allCities = existingCities.Values.ToList();

        // 3. Create areas from municipalities
        var municipalities = geoEntries
            .Where(g => !string.IsNullOrWhiteSpace(g.Kommune))
            .Select(g => g.Kommune!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m)
            .ToList();

        var areaFaker = new Faker();
        var areas = new List<Area>();
        var areaCount = Math.Min(options.AreaCount, municipalities.Count);
        var selectedMunicipalities = municipalities.Take(areaCount).ToList();

        foreach (var mun in selectedMunicipalities)
        {
            var areaCities = geoEntries
                .Where(g => string.Equals(g.Kommune, mun, StringComparison.OrdinalIgnoreCase))
                .Select(g => existingCities.GetValueOrDefault(g.Nr))
                .Where(c => c is not null)
                .Cast<City>()
                .Take(20)
                .ToList();

            var area = new Area
            {
                Id = Guid.NewGuid(),
                Name = mun.Length > 50 ? mun[..50] : mun,
                Description = $"Vacation area in {mun}",
                Status = EntityStatus.Published,
                Cities = areaCities,
                CreatedBy = StressTag
            };
            areas.Add(area);
        }
        db.Areas.AddRange(areas);
        await db.SaveChangesAsync(ct);
        totalEntities += areas.Count;
        logger.LogInformation("Created {Count} areas", areas.Count);

        // 4. Ensure features exist (reuse from seed)
        var features = await db.Features.ToListAsync(ct);
        if (features.Count == 0)
            return new StressDataResult(false, "No features found. Run SeedMinimal first.");

        var boolFeatures = features.Where(f => f.ValueType == FeatureValueType.Bool).ToList();
        var intFeatures = features.Where(f => f.ValueType == FeatureValueType.Int).ToList();

        // Feature probability map (key -> probability of having it)
        var featureProb = new Dictionary<string, double>
        {
            ["wifi"] = 0.92,
            ["dishwasher"] = 0.85,
            ["washing_machine"] = 0.70,
            ["nonsmoking"] = 0.80,
            ["dryer"] = 0.55,
            ["fireplace"] = 0.35,
            ["pet_friendly"] = 0.30,
            ["sauna"] = 0.20,
            ["pool"] = 0.15,
            ["spa"] = 0.10,
            ["aircondition"] = 0.08,
            ["ev_charger"] = 0.05,
            ["handicap"] = 0.08,
            ["water_view"] = 0.20,
        };

        // 5. Create season codes if missing
        var existingCodes = await db.SeasonCodes.Select(s => s.Code).ToListAsync(ct);
        var neededCodes = new List<SeasonCode>();
        if (!existingCodes.Contains("A"))
            neededCodes.Add(new SeasonCode { Code = "A", Name = "High season", Color = "#FF5733", SortOrder = 1 });
        if (!existingCodes.Contains("B"))
            neededCodes.Add(new SeasonCode { Code = "B", Name = "Mid season", Color = "#33C1FF", SortOrder = 2 });
        if (!existingCodes.Contains("C"))
            neededCodes.Add(new SeasonCode { Code = "C", Name = "Low season", Color = "#28A745", SortOrder = 3 });
        if (neededCodes.Count > 0)
        {
            db.SeasonCodes.AddRange(neededCodes);
            await db.SaveChangesAsync(ct);
        }

        // 6. Create season calendars
        var calendars = new List<SeasonCalendar>();
        var allSpans = new List<SeasonSpan>();
        var currentYear = DateTime.UtcNow.Year;
        var calendarYears = new[] { currentYear, currentYear + 1 };

        for (int i = 0; i < options.CalendarCount; i++)
        {
            var cal = new SeasonCalendar
            {
                Id = Guid.NewGuid(),
                Name = $"Calendar {i + 1}",
                IsTemplate = false,
                CreatedBy = StressTag
            };
            calendars.Add(cal);

            foreach (var year in calendarYears)
            {
                var winterEnd = DateTime.IsLeapYear(year) ? 29 : 28;
                // Winter: Jan-Feb → Low (C)
                allSpans.Add(new SeasonSpan { Id = Guid.NewGuid(), CalendarId = cal.Id, Code = "C", StartDate = new DateOnly(year, 1, 1), EndDate = new DateOnly(year, 2, winterEnd) });
                // Spring: Mar-May → Mid (B)
                allSpans.Add(new SeasonSpan { Id = Guid.NewGuid(), CalendarId = cal.Id, Code = "B", StartDate = new DateOnly(year, 3, 1), EndDate = new DateOnly(year, 5, 31) });
                // Summer: Jun-Aug → High (A)
                allSpans.Add(new SeasonSpan { Id = Guid.NewGuid(), CalendarId = cal.Id, Code = "A", StartDate = new DateOnly(year, 6, 1), EndDate = new DateOnly(year, 8, 31) });
                // Autumn: Sep-Oct → Mid (B)
                allSpans.Add(new SeasonSpan { Id = Guid.NewGuid(), CalendarId = cal.Id, Code = "B", StartDate = new DateOnly(year, 9, 1), EndDate = new DateOnly(year, 10, 31) });
                // Winter: Nov-Dec → Low (C)
                allSpans.Add(new SeasonSpan { Id = Guid.NewGuid(), CalendarId = cal.Id, Code = "C", StartDate = new DateOnly(year, 11, 1), EndDate = new DateOnly(year, 12, 31) });
            }
        }
        db.SeasonCalendars.AddRange(calendars);
        db.SeasonSpans.AddRange(allSpans);
        await db.SaveChangesAsync(ct);
        totalEntities += calendars.Count + allSpans.Count;
        logger.LogInformation("Created {CalCount} calendars with {SpanCount} spans", calendars.Count, allSpans.Count);

        // 7. Create house groups (1 per 50 houses, each linked to a calendar)
        var groupCount = Math.Max(1, options.HouseCount / 50);
        var groups = new List<HouseGroup>();
        for (int i = 0; i < groupCount; i++)
        {
            var cal = calendars[i % calendars.Count];
            groups.Add(new HouseGroup
            {
                Id = Guid.NewGuid(),
                Name = $"Group {i + 1}",
                DefaultCalendarId = cal.Id,
                CreatedBy = StressTag
            });
        }
        db.HouseGroups.AddRange(groups);
        await db.SaveChangesAsync(ct);
        totalEntities += groups.Count;

        // 8. Generate houses in batches
        var houseFaker = new Faker<VacationHouse>()
            .RuleFor(h => h.Id, f => Guid.NewGuid())
            .RuleFor(h => h.Title, (f, h) => f.Address.StreetName() + " " + f.Random.Number(1, 200))
            .RuleFor(h => h.Address, f => f.Address.StreetAddress())
            .RuleFor(h => h.Description, f => f.Lorem.Paragraphs(2))
            .RuleFor(h => h.SearchKeywords, f => string.Join(", ", f.Random.WordsArray(3, 6)))
            .RuleFor(h => h.Status, f => f.Random.WeightedRandom(
                new[] { EntityStatus.Published, EntityStatus.Draft, EntityStatus.Archived },
                new[] { 0.80f, 0.15f, 0.05f }))
            .RuleFor(h => h.PublishedAtUtc, (f, h) => h.Status >= EntityStatus.Published ? f.Date.Past(1).ToUniversalTime() : (DateTime?)null)
            .RuleFor(h => h.CreatedBy, _ => StressTag);

        var allHouseFeatures = new List<HouseFeatureValue>();
        var allPricePlans = new List<PricePlan>();
        var allSeasonPrices = new List<SeasonPrice>();
        var allAvailability = new List<AvailabilityBlock>();
        var allImages = new List<HouseImage>();

        const int batchSize = 200;
        var houseIndex = 0;

        while (houseIndex < options.HouseCount)
        {
            var batchCount = Math.Min(batchSize, options.HouseCount - houseIndex);
            var houses = houseFaker.Generate(batchCount);

            foreach (var house in houses)
            {
                var city = allCities[rng.Next(allCities.Count)];
                house.CityId = city.Id;
                house.GroupId = groups[rng.Next(groups.Count)].Id;

                // Assign to 1-2 areas
                var areaIdx = rng.Next(areas.Count);
                house.Areas = new List<Area> { areas[areaIdx] };
                if (rng.NextDouble() < 0.3 && areas.Count > 1)
                {
                    var secondIdx = (areaIdx + 1 + rng.Next(areas.Count - 1)) % areas.Count;
                    house.Areas.Add(areas[secondIdx]);
                }

                // Feature values
                var bedrooms = rng.Next(1, 7);
                var bathrooms = Math.Max(1, bedrooms / 2);
                var maxGuests = bedrooms * 2;
                var sizeM2 = 40 + bedrooms * 25 + rng.Next(-10, 20);

                allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = features.First(f => f.Key == "bedrooms").Id, RawValue = bedrooms.ToString() });
                allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = features.First(f => f.Key == "bathrooms").Id, RawValue = bathrooms.ToString() });
                allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = features.First(f => f.Key == "max_guests").Id, RawValue = maxGuests.ToString() });
                allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = features.First(f => f.Key == "size_m2").Id, RawValue = sizeM2.ToString() });

                foreach (var bf in boolFeatures)
                {
                    var prob = featureProb.GetValueOrDefault(bf.Key, 0.5);
                    if (rng.NextDouble() < prob)
                        allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = bf.Id, RawValue = "true" });
                }

                // Distance features
                var distShop = features.FirstOrDefault(f => f.Key == "distance_shop");
                var distWater = features.FirstOrDefault(f => f.Key == "distance_water");
                if (distShop is not null)
                    allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = distShop.Id, RawValue = (rng.Next(100, 5000)).ToString() });
                if (distWater is not null)
                    allHouseFeatures.Add(new HouseFeatureValue { HouseId = house.Id, FeatureId = distWater.Id, RawValue = (rng.Next(50, 3000)).ToString() });

                // Price plan
                var basePrice = 400 + bedrooms * 150 + rng.Next(-50, 100);
                var plan = new PricePlan
                {
                    Id = Guid.NewGuid(),
                    HouseId = house.Id,
                    Name = "Standard",
                    Currency = "DKK",
                    IsActive = true,
                    CreatedBy = StressTag
                };
                allPricePlans.Add(plan);

                allSeasonPrices.Add(new SeasonPrice { PricePlanId = plan.Id, Code = "A", NightlyPrice = basePrice * 1.5m });
                allSeasonPrices.Add(new SeasonPrice { PricePlanId = plan.Id, Code = "B", NightlyPrice = basePrice });
                allSeasonPrices.Add(new SeasonPrice { PricePlanId = plan.Id, Code = "C", NightlyPrice = basePrice * 0.7m });

                // Availability blocks (2-4 per house)
                var blockCount = rng.Next(2, 5);
                var blockStart = new DateOnly(currentYear, 1, 1);
                for (int b = 0; b < blockCount; b++)
                {
                    var daysOffset = rng.Next(30, 120);
                    var start = blockStart.AddDays(b * 90 + rng.Next(0, 30));
                    var end = start.AddDays(rng.Next(3, 21));
                    if (end > new DateOnly(currentYear + 1, 12, 31)) break;

                    var status = rng.NextDouble() switch
                    {
                        < 0.60 => AvailabilityStatus.Available,
                        < 0.80 => AvailabilityStatus.Blocked,
                        _ => AvailabilityStatus.Blocked
                    };
                    var source = status == AvailabilityStatus.Available ? AvailabilitySource.Manual : AvailabilitySource.Manual;

                    allAvailability.Add(new AvailabilityBlock
                    {
                        Id = Guid.NewGuid(),
                        HouseId = house.Id,
                        StartDate = start,
                        EndDate = end,
                        Status = status,
                        Source = source,
                        CreatedBy = StressTag
                    });
                }

                // Image metadata (3-7 per house, no actual files)
                var imgCount = rng.Next(3, 8);
                for (int img = 0; img < imgCount; img++)
                {
                    allImages.Add(new HouseImage
                    {
                        Id = Guid.NewGuid(),
                        HouseId = house.Id,
                        FileName = $"stress/{house.Id:N}_{img}.jpg",
                        Alt = $"House photo {img + 1}",
                        Kind = img == 0 ? ImageKind.Cover : ImageKind.Gallery
                    });
                }
            }

            db.Houses.AddRange(houses);
            await db.SaveChangesAsync(ct);
            totalEntities += houses.Count;
            houseIndex += batchCount;
            logger.LogInformation("Houses: {Done}/{Total}", houseIndex, options.HouseCount);
        }

        // Save feature values, pricing, availability, images in batches
        await SaveInBatchesAsync(db.HouseFeatures, allHouseFeatures, ct);
        totalEntities += allHouseFeatures.Count;
        logger.LogInformation("Created {Count} house feature values", allHouseFeatures.Count);

        await SaveInBatchesAsync(db.PricePlans, allPricePlans, ct);
        await SaveInBatchesAsync(db.SeasonPrices, allSeasonPrices, ct);
        totalEntities += allPricePlans.Count + allSeasonPrices.Count;
        logger.LogInformation("Created {Plans} price plans with {Prices} season prices", allPricePlans.Count, allSeasonPrices.Count);

        await SaveInBatchesAsync(db.AvailabilityBlocks, allAvailability, ct);
        totalEntities += allAvailability.Count;
        logger.LogInformation("Created {Count} availability blocks", allAvailability.Count);

        await SaveInBatchesAsync(db.Images, allImages, ct);
        totalEntities += allImages.Count;
        logger.LogInformation("Created {Count} image records", allImages.Count);

        sw.Stop();
        var msg = $"Stress data seeded: {totalEntities:N0} entities in {sw.Elapsed.TotalSeconds:F1}s";
        logger.LogInformation("{Msg}", msg);
        return new StressDataResult(true, msg, totalEntities);
    }

    public async Task<StressDataResult> ClearAsync(CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var total = 0;

        // Delete in dependency order
        total += await db.AvailabilityBlocks.Where(a => a.CreatedBy == StressTag).ExecuteDeleteAsync(ct);
        total += await db.Images.Where(i => i.FileName.StartsWith("stress/")).ExecuteDeleteAsync(ct);
        total += await db.HouseFeatures.Where(hf => db.Houses.Where(h => h.CreatedBy == StressTag).Select(h => h.Id).Contains(hf.HouseId)).ExecuteDeleteAsync(ct);
        total += await db.SeasonPrices.Where(sp => db.PricePlans.Where(p => p.CreatedBy == StressTag).Select(p => p.Id).Contains(sp.PricePlanId)).ExecuteDeleteAsync(ct);
        total += await db.PricePlans.Where(p => p.CreatedBy == StressTag).ExecuteDeleteAsync(ct);

        // Delete houses (cascades HouseAreas join table)
        total += await db.Houses.Where(h => h.CreatedBy == StressTag).ExecuteDeleteAsync(ct);

        // Delete season spans for stress calendars
        var stressCalIds = db.SeasonCalendars.Where(c => c.CreatedBy == StressTag).Select(c => c.Id);
        total += await db.SeasonSpans.Where(s => stressCalIds.Contains(s.CalendarId)).ExecuteDeleteAsync(ct);
        total += await db.SeasonCalendars.Where(c => c.CreatedBy == StressTag).ExecuteDeleteAsync(ct);

        // Delete groups and areas
        total += await db.HouseGroups.Where(g => g.CreatedBy == StressTag).ExecuteDeleteAsync(ct);
        total += await db.Areas.Where(a => a.CreatedBy == StressTag).ExecuteDeleteAsync(ct);
        total += await db.Cities.Where(c => c.CreatedBy == StressTag).ExecuteDeleteAsync(ct);

        sw.Stop();
        var msg = $"Stress data cleared: {total:N0} entities removed in {sw.Elapsed.TotalSeconds:F1}s";
        logger.LogInformation("{Msg}", msg);
        return new StressDataResult(true, msg, total);
    }

    private static async Task SaveInBatchesAsync<T>(DbSet<T> set, List<T> items, CancellationToken ct) where T : class
    {
        const int batch = 500;
        for (int i = 0; i < items.Count; i += batch)
        {
            var chunk = items.Skip(i).Take(batch);
            set.AddRange(chunk);
            await set.Entry(chunk.First()).Context.SaveChangesAsync(ct);
        }
    }

    private static List<GeoEntry> LoadBundledGeoData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("DanishGeoData.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return [];

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<List<GeoEntry>>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? [];
    }

    private sealed class GeoEntry
    {
        public string Nr { get; set; } = "";
        public string Navn { get; set; } = "";
        public string? Kommune { get; set; }
    }
}
