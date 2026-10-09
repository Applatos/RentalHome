using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Services.Admin.HouseGroups;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.DemoContent;

/// <summary>
/// The demo content's shared catalog: season codes, the house group with its season calendar,
/// and features. Unlike houses and areas, features are brought in line with the manifest
/// (name, category, unit, options, sort order), and an icon is added where one is missing.
/// </summary>
public sealed class DemoCatalogImporter(
    AppDbContext db,
    IAdminFeatureService features,
    IAdminPricingService pricing,
    IAdminHouseGroupService groups)
{
    // The admin services take a base URL only to build the URLs they return, which the import ignores.
    private const string NoBaseUrl = "";

    /// <returns>The house group's id.</returns>
    public async Task<Guid> ImportAsync(string root, DemoContentManifest manifest, DemoContentLog log, CancellationToken ct)
    {
        await EnsureSeasonCodesAsync(manifest.SeasonCodes, log, ct);
        var groupId = await EnsureGroupAsync(manifest.Group, log, ct);
        await EnsureFeaturesAsync(root, manifest.Features, log, ct);
        return groupId;
    }

    private async Task EnsureSeasonCodesAsync(IReadOnlyList<DemoSeasonCode> codes, DemoContentLog log, CancellationToken ct)
    {
        var existing = new HashSet<string>(
            await db.SeasonCodes.AsNoTracking().Select(c => c.Code).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var created = new List<string>();
        foreach (var code in codes.Where(c => !existing.Contains(c.Code)))
        {
            (await pricing.CreateSeasonCodeAsync(new SeasonCodeDto(code.Code, code.Name, code.Color, code.SortOrder), ct))
                .OrThrow($"Season code '{code.Code}'");
            created.Add($"{code.Code} ({code.Name})");
        }

        if (created.Count > 0)
            log.Changes.Add($"Season codes: created {string.Join(", ", created)}");
    }

    /// <summary>
    /// Creates the group if missing, and its calendar if the group has none or an empty one.
    /// </summary>
    private async Task<Guid> EnsureGroupAsync(DemoHouseGroup spec, DemoContentLog log, CancellationToken ct)
    {
        var group = await db.HouseGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Name == spec.Name, ct);
        var groupId = group?.Id
            ?? (await groups.CreateAsync(new HouseGroupDto(Guid.Empty, spec.Name), ct)).OrThrow($"House group '{spec.Name}'").Id;
        if (group is null)
            log.Changes.Add($"House group '{spec.Name}': created");

        var calendarId = group?.DefaultCalendarId;
        if (calendarId.HasValue && await db.SeasonSpans.AnyAsync(s => s.CalendarId == calendarId.Value, ct))
            return groupId;

        var firstYear = DateTime.UtcNow.Year;
        var spans = Enumerable.Range(firstYear, spec.Years)
            .SelectMany(year => spec.Seasons.Select(s => new SeasonSpanDto(Guid.Empty, Day(year, s.From), Day(year, s.To), s.Code)))
            .ToList();
        (await pricing.UpsertSeasonSpansAsync(groupId, spans, ct)).OrThrow($"Season calendar for '{spec.Name}'");
        log.Changes.Add($"Season calendar for '{spec.Name}': {firstYear}-{firstYear + spec.Years - 1}");
        return groupId;
    }

    private async Task EnsureFeaturesAsync(string root, IReadOnlyList<DemoFeature> specs, DemoContentLog log, CancellationToken ct)
    {
        var existing = await db.Features.ToDictionaryAsync(f => f.Key, StringComparer.Ordinal, ct);
        var created = new List<string>();
        var updated = new List<string>();
        var icons = 0;

        foreach (var spec in specs)
        {
            var dto = new UpsertFeatureDto(spec.Name, spec.NameEn, spec.Key, spec.ValueType, spec.Category,
                IsSearchable: true, Options: spec.Options, Unit: spec.Unit);

            Guid id;
            if (existing.TryGetValue(spec.Key, out var current))
            {
                id = current.Id;
                if (!Matches(current, spec))
                {
                    (await features.UpdateAsync(id, dto, ct)).OrThrow($"Feature '{spec.Key}'");
                    updated.Add(spec.Key);
                }
            }
            else
            {
                id = (await features.CreateAsync(dto, ct)).OrThrow($"Feature '{spec.Key}'");
                created.Add(spec.Key);
            }

            // The admin services cannot set the sort order.
            var feature = await db.Features.FirstAsync(f => f.Id == id, ct);
            if (feature.SortOrder != spec.SortOrder)
            {
                feature.SortOrder = spec.SortOrder;
                await db.SaveChangesAsync(ct);
                if (!created.Contains(spec.Key) && !updated.Contains(spec.Key))
                    updated.Add(spec.Key);
            }

            if (feature.IconUrl is null && spec.Icon is not null)
            {
                using var upload = new DemoContentUploads([DemoContentFiles.Resolve(root, $"features/{spec.Icon}")]);
                (await features.UploadIconAsync(id, upload.Files[0], NoBaseUrl, ct)).OrThrow($"Icon for feature '{spec.Key}'");
                icons++;
            }
        }

        if (created.Count > 0)
            log.Changes.Add($"Features: created {string.Join(", ", created)}");
        if (updated.Count > 0)
            log.Changes.Add($"Features: updated {string.Join(", ", updated)}");
        if (icons > 0)
            log.Changes.Add($"Features: added {icons} icons");
    }

    private static bool Matches(Feature feature, DemoFeature spec)
        => feature.Name == spec.Name
           && feature.NameEn == spec.NameEn
           && feature.ValueType.ToString().Equals(spec.ValueType, StringComparison.OrdinalIgnoreCase)
           && feature.Category.ToString().Equals(spec.Category, StringComparison.OrdinalIgnoreCase)
           && feature.Unit == spec.Unit
           && feature.Options == spec.Options
           && feature.IsSearchable;

    private static DateOnly Day(int year, string monthDay)
        => DateOnly.ParseExact($"{year:D4}-{monthDay}", "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
