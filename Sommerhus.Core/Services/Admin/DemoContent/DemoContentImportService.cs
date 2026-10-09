using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Houses;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Services.Admin.Lifecycle;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.DemoContent;

/// <remarks>
/// Houses and areas are created directly so they get the manifest's fixed ids, which is how a
/// later run recognises them even after an admin renames them. Everything after that goes
/// through the admin services, so the import gets their validation and side effects: search
/// documents, from-prices and quote cache invalidation.
/// </remarks>
public sealed class DemoContentImportService(
    AppDbContext db,
    DemoCatalogImporter catalog,
    IAdminHouseFeatureService houseFeatures,
    IAdminHouseImageService houseImages,
    IAdminAreaImageService areaImages,
    IAdminHousePricingService housePricing,
    IEntityLifecycleService lifecycle,
    IOptions<DemoContentOptions> options) : IDemoContentImportService
{
    // Two runs at once would both see the same content as missing.
    private static readonly SemaphoreSlim Gate = new(1, 1);

    // The admin services take a base URL only to build the URLs they return, which the import ignores.
    private const string NoBaseUrl = "";

    private readonly DemoContentLog log = new();

    public async Task<DemoContentImportResultDto> ImportAsync(CancellationToken ct)
    {
        await Gate.WaitAsync(ct);
        try
        {
            var root = options.Value.ResolvePath();
            var manifest = DemoContentManifest.Load(root);

            var groupId = await catalog.ImportAsync(root, manifest, log, ct);
            foreach (var area in manifest.Areas)
                await EnsureAreaAsync(root, area, ct);
            foreach (var house in manifest.Houses)
                await EnsureHouseAsync(root, house, manifest.Areas, groupId, ct);

            return new DemoContentImportResultDto(true, log.Changes, log.Notes, null);
        }
        catch (DemoContentException ex)
        {
            return new DemoContentImportResultDto(false, log.Changes, log.Notes, ex.Message);
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// Creates and publishes the area if missing; adds images if it has none.
    /// </summary>
    private async Task EnsureAreaAsync(string root, DemoArea spec, CancellationToken ct)
    {
        var area = await db.Areas.Include(a => a.AreaImages).FirstOrDefaultAsync(a => a.Id == spec.Id, ct);
        var created = area is null;
        if (area is null)
        {
            if (await db.Areas.AnyAsync(a => a.Name == spec.Name, ct))
            {
                log.Notes.Add($"Area '{spec.Name}': an area with this name already exists and was left as it is.");
                return;
            }

            var zips = spec.Zips ?? [];
            var cities = await db.Cities.Where(c => zips.Contains(c.Zip)).ToListAsync(ct);
            if (cities.Count != zips.Count)
                throw new DemoContentException($"Area '{spec.Name}': unknown postal code in {string.Join(", ", zips)}.");

            area = new Area
            {
                Id = spec.Id,
                Name = spec.Name,
                NameEn = spec.NameEn,
                Description = DemoContentFiles.ReadDescriptionText(root, spec.Text, spec.Name),
                Cities = cities
            };
            db.Areas.Add(area);
            await db.SaveChangesAsync(ct);
        }

        var parts = new List<string>();
        var images = area.AreaImages.Count == 0 ? CoverFirst(DemoContentFiles.ListImages(root, spec.ImageFolder), spec.Cover) : [];
        if (images.Count > 0)
        {
            using var upload = new DemoContentUploads(images);
            (await areaImages.UploadAsync(area.Id, upload.Files, NoBaseUrl, ct)).OrThrow($"Images for area '{spec.Name}'");
            parts.Add($"{images.Count} images");
        }

        if (created)
            (await lifecycle.TransitionAreaAsync(area.Id, EntityStatus.Published, ct)).OrThrow($"Publishing area '{spec.Name}'");

        log.Report("Area", spec.Name, created, parts, published: created);
    }

    /// <summary>
    /// Creates the house if missing, then adds whichever of features, images and prices it has
    /// none of, and publishes it if it has never been published.
    /// </summary>
    private async Task EnsureHouseAsync(string root, DemoHouse spec, IReadOnlyList<DemoArea> areaSpecs, Guid groupId, CancellationToken ct)
    {
        var house = await db.Houses.FirstOrDefaultAsync(h => h.Id == spec.Id, ct);
        var created = house is null;
        if (house is null)
        {
            if (await db.Houses.AnyAsync(h => h.Title == spec.Title, ct))
            {
                log.Notes.Add($"House '{spec.Title}': a house with this title already exists and was left as it is.");
                return;
            }

            house = new VacationHouse
            {
                Id = spec.Id,
                Title = spec.Title,
                Address = spec.Address,
                CityId = await CityIdAsync(spec, ct),
                Description = DemoContentFiles.ReadDescriptionText(root, spec.Text, spec.Title),
                SearchKeywords = spec.SearchKeywords,
                GroupId = groupId,
                Areas = await AreasAsync(spec, areaSpecs, ct)
            };
            db.Houses.Add(house);
            await db.SaveChangesAsync(ct);
        }

        var parts = new List<string>();
        if (spec.Features.Count > 0 && !await db.HouseFeatures.AnyAsync(f => f.HouseId == house.Id, ct))
        {
            await AddHouseFeaturesAsync(spec, house.Id, ct);
            parts.Add($"{spec.Features.Count} features");
        }

        if (!await db.Images.AnyAsync(i => i.HouseId == house.Id, ct))
        {
            var count = await AddHouseImagesAsync(root, spec, house.Id, ct);
            if (count > 0)
                parts.Add($"{count} images");
        }

        if (!await db.PricePlans.AnyAsync(p => p.HouseId == house.Id, ct))
        {
            var prices = spec.Prices.Select(p => new SeasonPriceDto(Guid.Empty, Guid.Empty, p.Key, p.Value)).ToList();
            var plan = new PricePlanDetailsDto(Guid.Empty, house.Id, "Standard", "DKK", true, DateTime.UtcNow, null, prices);
            (await housePricing.UpsertPricingAsync(house.Id, plan, ct)).OrThrow($"Prices for house '{spec.Title}'");
            parts.Add("price plan");
        }

        // A house that was published before and then taken down was taken down on purpose.
        var publish = house.Status == EntityStatus.Draft && house.PublishedAtUtc is null;
        if (publish)
            (await lifecycle.TransitionHouseAsync(house.Id, EntityStatus.Published, ct)).OrThrow($"Publishing house '{spec.Title}'");

        log.Report("House", spec.Title, created, parts, publish);
    }

    private async Task AddHouseFeaturesAsync(DemoHouse spec, Guid houseId, CancellationToken ct)
    {
        var keys = spec.Features.Keys.ToList();
        var ids = await db.Features.Where(f => keys.Contains(f.Key)).ToDictionaryAsync(f => f.Key, f => f.Id, ct);
        var unknown = keys.Where(k => !ids.ContainsKey(k)).ToList();
        if (unknown.Count > 0)
            throw new DemoContentException($"House '{spec.Title}': unknown feature {string.Join(", ", unknown)}.");

        var values = spec.Features.Select(f => new PostFeatureValueDto(ids[f.Key], f.Value)).ToList();
        (await houseFeatures.UpsertFeaturesAsync(houseId, values, ct)).OrThrow($"Features for house '{spec.Title}'");
    }

    private async Task<int> AddHouseImagesAsync(string root, DemoHouse spec, Guid houseId, CancellationToken ct)
    {
        var all = DemoContentFiles.ListImages(root, spec.ImageFolder, spec.SkipImages);
        if (all.Count == 0)
            return 0;

        string? Find(string? name) => name is null
            ? null
            : all.FirstOrDefault(f => Path.GetFileName(f).Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new DemoContentException($"House '{spec.Title}': image '{name}' not found in {spec.ImageFolder}.");

        var cover = Find(spec.Cover);
        var floorplan = Find(spec.Floorplan);
        var ordered = new[] { cover }
            .Concat(all.Where(f => f != cover && f != floorplan))
            .Append(floorplan)
            .OfType<string>()
            .ToList();

        using var upload = new DemoContentUploads(ordered);
        var uploaded = (await houseImages.UploadAsync(houseId, upload.Files, NoBaseUrl, ct)).OrThrow($"Images for house '{spec.Title}'");

        // The upload returns the images in the order they were sent.
        if (cover is not null)
            (await houseImages.SetKindAsync(houseId, uploaded[0].Id, ImageKind.Cover, ct)).OrThrow($"Cover for house '{spec.Title}'");
        if (floorplan is not null)
            (await houseImages.SetKindAsync(houseId, uploaded[^1].Id, ImageKind.Floorplan, ct)).OrThrow($"Floor plan for house '{spec.Title}'");

        return ordered.Count;
    }

    // Uploads are numbered in the order they are sent, so the cover goes first.
    private static IReadOnlyList<string> CoverFirst(IReadOnlyList<string> files, string? cover)
        => cover is null
            ? files
            : files.OrderBy(f => Path.GetFileName(f).Equals(cover, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ToList();

    private async Task<Guid> CityIdAsync(DemoHouse spec, CancellationToken ct)
        => await db.Cities.Where(c => c.Zip == spec.Zip).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct)
            ?? throw new DemoContentException($"House '{spec.Title}': unknown postal code {spec.Zip}.");

    /// <summary>
    /// An area is the manifest's own one when it exists, otherwise any area with that name.
    /// </summary>
    private async Task<List<Area>> AreasAsync(DemoHouse spec, IReadOnlyList<DemoArea> areaSpecs, CancellationToken ct)
    {
        var areas = new List<Area>();
        foreach (var name in spec.Areas ?? [])
        {
            var id = areaSpecs.FirstOrDefault(a => a.Name == name)?.Id;
            var area = (id.HasValue ? await db.Areas.FirstOrDefaultAsync(a => a.Id == id.Value, ct) : null)
                ?? await db.Areas.FirstOrDefaultAsync(a => a.Name == name, ct)
                ?? throw new DemoContentException($"House '{spec.Title}': unknown area '{name}'.");
            areas.Add(area);
        }

        return areas;
    }
}
