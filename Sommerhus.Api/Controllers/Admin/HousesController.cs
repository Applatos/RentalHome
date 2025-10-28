using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Data;
using Sommerhus.Api.Models;
using Sommerhus.Api.Utils;
using Sommerhus.Contracts.Dtos.Admin.Houses;
using Sommerhus.Contracts.Dtos.Admin.Features;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Models;
using System.Numerics;

namespace Sommerhus.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/houses")]
public sealed class HousesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PageResult<HouseListItemDto>> Search([FromQuery] string? query, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var q = db.Houses.AsNoTracking()
            .Include(h => h.City)
            .Include(h => h.Areas)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(h =>
                (h.Title != null && EF.Functions.Like(h.Title, $"%{term}%")) ||
                (h.City != null && h.City.Name != null && EF.Functions.Like(h.City.Name, $"%{term}%")) ||
                (h.City != null && h.City.Zip != null && EF.Functions.Like(h.City.Zip, $"%{term}%")));
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(h => h.CreatedUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(h => new HouseListItemDto(
                h.Id,
                h.Title,
                h.City != null ? $"{h.City.Zip} – {h.City.Name}" : string.Empty,
                h.Areas.OrderBy(a => a.Name).Select(a => a.Name).ToList(),
                null,
                h.CreatedUtc))
            .ToListAsync(ct);

        return new PageResult<HouseListItemDto>
        {
            Query = query ?? "",
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HouseDetailsDto>> Get(Guid id, CancellationToken ct)
    {
        var h = await db.Houses
            .Include(x => x.Images)
            .Include(x => x.City)
            .Include(x => x.Areas)
            .Include(x => x.HouseFeatures).ThenInclude(x => x.Feature)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (h is null) return NotFound();

        var images = h.Images
            .OrderBy(i => i.Kind == ImageKind.Cover ? 0 : (i.Kind == ImageKind.Gallery ? 1 : 2))
            .Select(i => new ImageDto(i.Id, UrlBuilder.ToAbsolute(Request, UrlBuilder.HouseImageWebPath(i.HouseId, i.FileName)), i.Alt, i.Kind.ToString()))
            .ToList();

        var features = h.HouseFeatures
        .Select(hf =>
        {
            var f = hf.Feature;
            return new FeatureValueDto(
                Id: hf.FeatureId,
                Name: f.Name,
                ValueType: f.ValueType.ToString(),
                Unit: f.Unit,
                IconUrl: f.IconUrl,
                RawValue: hf.RawValue
            );
        })
        .ToList();

        var plan = await db.RatePlans
            .AsNoTracking()
            .Include(rp => rp.Seasons)
            .Include(rp => rp.Modifiers)
            .Where(rp => rp.HouseId == h.Id && rp.IsActive)
            .OrderByDescending(rp => rp.IsActive)
            .ThenByDescending(rp => rp.UpdatedUtc ?? rp.CreatedUtc)
            .FirstOrDefaultAsync(ct);

        var planDto = plan is not null ? MapPlan(plan) : null;


        var areaItems = h.Areas
            .OrderBy(a => a.Name)
            .Select(a => new LookupItem(a.Id, a.Name))
            .ToList();

        var areaIds = areaItems.Select(a => a.Id).ToList();

        return new HouseDetailsDto(
            h.Id,
            h.Title,
            h.CityId,
            h.City != null ? $"{h.City.Zip} – {h.City.Name}" : string.Empty,
            areaIds,
            areaItems,
            h.Address,
            h.Description,
            h.CreatedUtc,
            features,
            images,
            planDto);
    }


    // POST: /api/admin/houses
    [HttpPost]
    public async Task<ActionResult<Guid>> Create([FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        var entity = new VacationHouse
        {
            Id = Guid.NewGuid(),
            Title = dto.Name,
            Address = dto.Address,
            CityId = dto.CityId,
            Description = dto.Description,
            CreatedUtc = DateTime.UtcNow
        };


        var requestedAreaIds = dto.AreaIds?.Where(aid => aid != Guid.Empty).Distinct().ToList() ?? new();
        if (dto.AreaIds is not null && dto.AreaIds.Any(aid => aid == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        if (requestedAreaIds.Count > 0)
        {
            var areas = await db.Areas.Where(a => requestedAreaIds.Contains(a.Id)).ToListAsync(ct);
            if (areas.Count != requestedAreaIds.Count)
            {
                ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
                return ValidationProblem(ModelState);
            }

            foreach (var area in areas)
            {
                entity.Areas.Add(area);
            }
        }

        db.Houses.Add(entity);
        await db.SaveChangesAsync(ct);

        // AdminApiClient forventer et Guid tilbage
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity.Id);
    }

    // PUT: /api/admin/houses/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertHouseDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var h = await db.Houses
               .Include(x => x.Areas)
               .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        h.Title = dto.Name;
        h.Address = dto.Address;
        h.CityId = dto.CityId;
        h.Description = dto.Description;

        var requestedAreaIds = dto.AreaIds?.Where(aid => aid != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (dto.AreaIds is not null && dto.AreaIds.Any(aid => aid == Guid.Empty))
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        var areas = requestedAreaIds.Count > 0
            ? await db.Areas.Where(a => requestedAreaIds.Contains(a.Id)).ToListAsync(ct)
            : new List<Area>();

        if (areas.Count != requestedAreaIds.Count)
        {
            ModelState.AddModelError(nameof(dto.AreaIds), "Ukendt område");
            return ValidationProblem(ModelState);
        }

        h.Areas.Clear();
        foreach (var area in areas)
        {
            h.Areas.Add(area);
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }


    // DELETE: /api/admin/houses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var h = await db.Houses.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (h is null) return NotFound();

        db.Houses.Remove(h);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }


    [HttpPost("{houseId:guid}/features")]
    public async Task<IActionResult> UpsertFeatures(Guid houseId, [FromBody] IEnumerable<PostFeatureValueDto>? values, CancellationToken ct)
    {

        if (values is null)
            return BadRequest("Feature values are required.");

        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
            return NotFound();

        // Normalize and filter incoming values
        var normalized = values
            .Where(v => v != null)
            .Select(v => new { v.FeatureId, Raw = (v.RawValue ?? string.Empty).Trim() })
            .Where(x => x.FeatureId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Raw))
            .GroupBy(x => x.FeatureId)
            .Select(g => new HouseFeatureValue
            {
                HouseId = houseId,
                FeatureId = g.Key,
                RawValue = g.First().Raw
            })
            .ToList();

        var featureIds = normalized.Select(i => i.FeatureId).Distinct().ToList();
        if (featureIds.Count > 0)
        {
            var existingFeatureIds = await db.Features
                .AsNoTracking()
                .Where(f => featureIds.Contains(f.Id))
                .Select(f => f.Id)
                .ToListAsync(ct);

            var missing = featureIds.Except(existingFeatureIds).ToList();
            if (missing.Any())
            {
                return BadRequest(new { Message = "Some features do not exist.", MissingFeatureIds = missing });
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Efficient server-side delete of existing values for this house
            await db.HouseFeatures.Where(hf => hf.HouseId == houseId).ExecuteDeleteAsync(ct);

            if (normalized.Count > 0)
            {
                await db.HouseFeatures.AddRangeAsync(normalized, ct);
                await db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return NoContent();
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpPut("{houseId:guid}/pricing")]
    public async Task<IActionResult> UpsertPricing(Guid houseId, [FromBody] UpsertRatePlanDto dto, CancellationToken ct)
    {
        var houseExists = await db.Houses.AsNoTracking().AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
            return NotFound();
        // Validate and normalize the incoming DTO
        var (planName, currency, seasons) = NormalizeRatePlan(dto);

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        // If updating an existing plan, verify it exists
        var plan = await db.RatePlans
            .Include(p => p.Seasons)
            .Where(p => p.HouseId == houseId && p.Id == dto.PlanId)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.UpdatedUtc ?? p.CreatedUtc)
            .FirstOrDefaultAsync(ct);

        // If no PlanId provided, get the latest active or most recent plan
        if (plan is null)
        {
            plan = await db.RatePlans
                .Include(p => p.Seasons)
                .Where(p => p.HouseId == houseId)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.UpdatedUtc ?? p.CreatedUtc)
                .FirstOrDefaultAsync(ct);
        }

        // If no existing plan found, create a new one
        if (plan is null)
        {
            plan = new RatePlan
            {
                Id = dto.PlanId ?? Guid.NewGuid(),
                HouseId = houseId,
                CreatedUtc = DateTime.UtcNow
            };
            db.RatePlans.Add(plan);
        }

        // If PlanId is provided but not found, return 404
        if (dto.PlanId.HasValue && plan is null)
        {
            return NotFound();
        }

        plan.Name = planName;
        plan.Currency = currency;
        plan.IsActive = dto.IsActive;
        plan.UpdatedUtc = DateTime.UtcNow;

        await db.RateSeasons
            .Where(s => s.RatePlanId == plan.Id)
            .ExecuteDeleteAsync(ct);

        if (seasons.Count > 0)
        {
            var entities = seasons.Select(s => new RateSeason
            {
                Id = s.Id ?? Guid.NewGuid(),
                RatePlanId = plan.Id,
                Name = s.Name,
                StartDate = s.Start,
                EndDate = s.End,
                NightlyPrice = s.Price,
                MinStayNights = s.MinStay
            }).ToList();

            await db.RateSeasons.AddRangeAsync(entities, ct);
        }

        await db.SaveChangesAsync(ct);

        var refreshed = await db.RatePlans
            .AsNoTracking()
            .Include(p => p.Seasons)
            .FirstAsync(p => p.Id == plan.Id, ct);

        return Ok(MapPlan(refreshed));
    }

    private static RatePlanDetailsDto MapPlan(RatePlan plan)
    {
        var seasons = plan.Seasons
            .OrderBy(s => s.StartDate)
            .Select(s => new RateSeasonDetailsDto(
                s.Id,
                s.RatePlanId,
                s.Name,
                s.StartDate,
                s.EndDate,
                s.NightlyPrice,
                s.MinStayNights))
            .ToList();

        return new RatePlanDetailsDto(
            plan.Id,
            plan.HouseId,
            plan.Name,
            plan.Currency,
            plan.IsActive,
            plan.CreatedUtc,
            plan.UpdatedUtc,
            seasons);
    }

    private (string Name, string Currency, List<NormalizedSeason> Seasons) NormalizeRatePlan(UpsertRatePlanDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError(nameof(dto.Name), "Planens navn er påkrævet.");
        }
        else if (name.Length > 100)
        {
            ModelState.AddModelError(nameof(dto.Name), "Planens navn må højst være 100 tegn.");
        }

        var currency = (dto.Currency ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3 || currency.Any(c => !char.IsLetter(c)))
        {
            ModelState.AddModelError(nameof(dto.Currency), "Valuta skal være en ISO-kode med 3 bogstaver.");
        }

        var seasons = NormalizeSeasons(dto);
        return (name, currency, seasons);
    }

    private List<NormalizedSeason> NormalizeSeasons(UpsertRatePlanDto dto)
    {
        var normalized = new List<NormalizedSeason>();

        if (dto.Seasons is null || dto.Seasons.Count == 0)
        {
            ModelState.AddModelError(nameof(dto.Seasons), "Tilføj mindst én sæson.");
            return normalized;
        }

        for (var index = 0; index < dto.Seasons.Count; index++)
        {
            var season = dto.Seasons[index];
            if (season is null)
            {
                ModelState.AddModelError($"{nameof(dto.Seasons)}[{index}]", "Sæson er påkrævet.");
                continue;
            }

            var name = season.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError($"{nameof(dto.Seasons)}[{index}].Name", "Navn er påkrævet.");
                continue;
            }

            var start = season.StartDate;
            var end = season.EndDate;
            if (end < start)
            {
                ModelState.AddModelError($"{nameof(dto.Seasons)}[{index}]", $"Slutdato skal være efter startdato for '{name}'.");
                continue;
            }

            var price = decimal.Round(season.NightlyPrice, 2, MidpointRounding.AwayFromZero);
            if (price <= 0)
            {
                ModelState.AddModelError($"{nameof(dto.Seasons)}[{index}].NightlyPrice", $"Pris for '{name}' skal være positiv.");
                continue;
            }

            int? minStay = null;
            if (season.MinStayNights is not null)
            {
                if (season.MinStayNights < 1)
                {
                    ModelState.AddModelError($"{nameof(dto.Seasons)}[{index}].MinStayNights", $"Minimumsnætter for '{name}' skal være mindst 1.");
                    continue;
                }

                minStay = season.MinStayNights;
            }

            normalized.Add(new NormalizedSeason(season.Id, name, start, end, price, minStay));
        }

        var ordered = normalized.OrderBy(s => s.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            var prev = ordered[i - 1];
            var current = ordered[i];
            if (current.Start <= prev.End)
            {
                ModelState.AddModelError(nameof(UpsertRatePlanDto.Seasons), $"Sæson '{current.Name}' overlapper med '{prev.Name}'.");
                break;
            }
        }

        return normalized;
    }

    public record NormalizedSeason(Guid? Id, string Name, DateOnly Start, DateOnly End, decimal Price, int? MinStay);
}
