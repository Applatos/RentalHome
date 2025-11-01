using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Sommerhus.Api.Data;
using Sommerhus.Contracts.Dtos.Admin.Pricing;
using Sommerhus.Contracts.Dtos.Shared;
using Sommerhus.Pricing.Abstractions;
using Sommerhus.Pricing.Models;
using System.Linq;
using System.Text.RegularExpressions;

namespace Sommerhus.Api.Controllers;

[ApiController]
[Route("api")]
public class PricingController : ControllerBase
{
    private readonly IConfiguration _configuration;
    // Change the type of _db from DbContext to AppDbContext
    private readonly AppDbContext _db;

    // Fix for CS1061: 'DbContext' does not contain a definition for 'SeasonSpans'
    // You need to use your actual derived DbContext type (AppDbContext) to access DbSet properties.

    public PricingController(IConfiguration configuration, AppDbContext db)
    {
        _configuration = configuration;
        _db = db;
    }

    [HttpPost("pricing/quote")]
    public async Task<ActionResult<PriceQuoteResponseDto>> Quote([FromBody] PriceQuoteRequestDto requestDto, [FromServices] IPricingPipeline pipeline, CancellationToken ct)
    {
        if (!_configuration.GetValue("Pricing:EnabledV1", true))
        {
            return StatusCode(503, "Pricing midlertidigt deaktiveret");
        }

        if (requestDto.Arrival >= requestDto.Departure)
        {
            return BadRequest("Ugyldigt dato-interval");
        }

        var request = new PriceQuoteRequestDto(requestDto.HouseId, requestDto.Arrival, requestDto.Departure, requestDto.Guests, requestDto.AreaId);
        var result = await pipeline.QuoteAsync(request, ct);

        var response = new PriceQuoteResponseDto(
            result.Currency,
            result.Nights,
            result.Items.Select(i => new PriceQuoteLineItemDto(i.Code, i.Text, i.Amount)).ToList(),
            result.Subtotal,
            result.Tax,
            result.Total);

        return Ok(response);
    }

    [HttpGet("/admin/calendar/groups/{groupId:guid}/spans")]
    public async Task<ActionResult<IEnumerable<SeasonSpanDto>>>GetGroupSpans(Guid groupId, CancellationToken ct)
    {
        var spans = await _db.SeasonSpans
            .AsNoTracking()
            .Where(s => s.GroupId == groupId)
            .ToListAsync(ct);
        var result = spans.Select(s => new SeasonSpanDto(
            s.Id,
            s.GroupId,
            s.StartDate,
            s.EndDate,
            s.Code)).ToList();
        return Ok(result);
    }

    [HttpPut("admin/calendar/group/{groupId:guid}/spans")]
    public async Task<ActionResult<IEnumerable<SeasonSpanDto>>> UpsertGroupSpans(Guid groupId, [FromBody] IReadOnlyList<SeasonSpanDto> spans, CancellationToken ct)
    {
        var groupExists = await _db.HouseGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == groupId, ct);
        if (!groupExists)
            return NotFound("Group not found.");


        // 2) Basal validering
        if (spans is null || spans.Count == 0)
            return BadRequest("Angiv mindst ét span.");

        // 2a) Dato- og kode-validering
        foreach (var s in spans)
        {
            if (s.EndDate < s.StartDate)
                return BadRequest("EndDate skal være >= StartDate.");
            var codeExists = await _db.SeasonCodes.AsNoTracking()
                .AnyAsync(c => c.Code == s.Code, ct);
            if (!codeExists)
                return BadRequest($"Ukendt kode: {s.Code}");
        }

        // 2b) Overlap-validering (enkelt og effektivt)
        var sorted = spans
            .OrderBy(s => s.StartDate)
            .ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            // overlapper hvis næste.Start <= nuværende.End
            if (sorted[i].StartDate <= sorted[i - 1].EndDate)
                return BadRequest("Spans må ikke overlappe (i samme gruppe).");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try {             // 3) Slet eksisterende spans for gruppen
            var existingSpans = _db.SeasonSpans.Where(s => s.GroupId == groupId);
            _db.SeasonSpans.RemoveRange(existingSpans);
            await _db.SaveChangesAsync(ct);

            // 4) Indsæt nye spans
            var newSpans = spans.Select(s => new SeasonSpan {Id = s.Id, GroupId = groupId, StartDate = s.StartDate, EndDate = s.EndDate, Code = s.Code}).ToList();
            await _db.SeasonSpans.AddRangeAsync(newSpans, ct);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            var result = newSpans.Select(s => new SeasonSpanDto(
                s.Id,
                s.GroupId,
                s.StartDate,
                s.EndDate,
                s.Code)).ToList();
            return Ok(result);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    [HttpGet("admin/pricing/plans/{houseId:guid}")]
    public async Task<ActionResult<IEnumerable<PricePlanDetailsDto>>> listPricePlans(Guid houseId, CancellationToken ct)
    {
        var plans = await _db.PricePlans
            .AsNoTracking()
            .Where(p => p.HouseId == houseId)
            .Include(p => p.SeasonPrices)
            .ToListAsync(ct);

        var result = plans.Select(p => new PricePlanDetailsDto(
            p.Id,
            p.HouseId,
            p.Name,
            p.Currency,
            p.IsActive,
            p.CreatedUtc,
            p.UpdatedUtc,
            p.SeasonPrices.Select(sp => new SeasonPriceDto(
                sp.Id,
                sp.PricePlanId,
                sp.Code,
                sp.NightlyPrice)).ToList()
        )).ToList();
        return Ok(result);
    }


    // POST /api/admin/pricing/plans/{planId}/activate
    [HttpPost("pricing/plans/{planId:guid}/activate")]
    public async Task<IActionResult> ActivatePlan(Guid planId, CancellationToken ct)
    {
        var plan = await _db.PricePlans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return NotFound("Plan not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Deaktivér alle andre planer for huset (server-side bulk update)
            await _db.PricePlans
                .Where(p => p.HouseId == plan.HouseId && p.Id != plan.Id && p.IsActive)
                .ExecuteUpdateAsync(up => up
                    .SetProperty(p => p.IsActive, false)
                    .SetProperty(p => p.UpdatedUtc, DateTime.UtcNow), ct);

            // Aktivér målet (kun hvis ikke allerede aktiv)
            if (!plan.IsActive)
            {
                plan.IsActive = true;
                plan.UpdatedUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            await tx.CommitAsync(ct);
            return NoContent(); // 204 er fint til “state change”
            // Alternativ: return Ok(new RatePlanSummaryDto(...)) hvis UI vil have plan-snapshot tilbage
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(ct);
            // Hvis du har et filtreret unikt indeks (HouseId, IsActive=1) kan du ramme conflicts her.
            return Conflict("Kun én aktiv plan pr. hus er tilladt.");
        }
    }

    // GET /api/admin/season-codes
    [HttpGet("season-codes")]
    public async Task<ActionResult<IEnumerable<SeasonCodeDto>>> ListSeasonCodes(CancellationToken ct)
    {
        var rows = await _db.SeasonCodes
            .AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .Select(c => new SeasonCodeDto(c.Code, c.Name, c.Color, c.SortOrder))
            .ToListAsync(ct);

        return Ok(rows);
    }


    [HttpPost("season-codes")]
    public async Task<ActionResult<SeasonCodeDto>> CreateSeasonCode([FromBody] SeasonCodeDto dto, CancellationToken ct)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Code))
        {
            return BadRequest("Kode er påkrævet.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();

        var exists = await _db.SeasonCodes
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, ct);

        if (exists)
        {
            return Conflict($"Sæsonkoden '{code}' findes allerede.");
        }

        string? color = null;
        if (!string.IsNullOrWhiteSpace(dto.Color))
        {
            var normalized = dto.Color.Trim();
            if (!Regex.IsMatch(normalized, "^#?[0-9A-Fa-f]{6}$"))
            {
                return BadRequest("Farvekoden skal være et hex-format på 6 cifre.");
            }

            color = normalized.StartsWith("#", StringComparison.Ordinal)
                ? normalized.ToUpperInvariant()
                : $"#{normalized.ToUpperInvariant()}";
        }

        var sortOrder = Math.Max(0, dto.SortOrder);

        var entity = new SeasonCode
        {
            Code = code,
            Name = string.IsNullOrWhiteSpace(dto.Label) ? code : dto.Label.Trim(),
            Color = color ?? "#6C757D",
            SortOrder = sortOrder
        };

        await _db.SeasonCodes.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);

        var result = new SeasonCodeDto(entity.Code, entity.Name, entity.Color, entity.SortOrder);
        return Created($"season-codes/{entity.Code}", result);
    }

}