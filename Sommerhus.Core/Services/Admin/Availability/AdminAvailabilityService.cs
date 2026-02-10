using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Dtos.Shared;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Availability;

public sealed class AdminAvailabilityService(AppDbContext db) : IAdminAvailabilityService
{
    public async Task<ServiceResult<IReadOnlyList<AvailabilityBlockDto>>> GetBlocksAsync(
        Guid houseId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var houseExists = await db.Houses.AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
            return ServiceResult<IReadOnlyList<AvailabilityBlockDto>>.NotFound();

        var blocks = await db.AvailabilityBlocks
            .AsNoTracking()
            .Where(b => b.HouseId == houseId && b.StartDate < to && b.EndDate > from)
            .OrderBy(b => b.StartDate)
            .Select(b => ToDto(b))
            .ToListAsync(ct);

        return ServiceResult<IReadOnlyList<AvailabilityBlockDto>>.Success(blocks);
    }

    public async Task<ServiceResult<AvailabilityBlockDto>> GetBlockAsync(Guid blockId, CancellationToken ct)
    {
        var block = await db.AvailabilityBlocks
            .AsNoTracking()
            .Where(b => b.Id == blockId)
            .Select(b => ToDto(b))
            .FirstOrDefaultAsync(ct);

        return block is null
            ? ServiceResult<AvailabilityBlockDto>.NotFound()
            : ServiceResult<AvailabilityBlockDto>.Success(block);
    }

    public async Task<ServiceResult<AvailabilityBlockDto>> CreateBlockAsync(
        Guid houseId, UpsertAvailabilityBlockDto dto, CancellationToken ct)
    {
        var houseExists = await db.Houses.AnyAsync(h => h.Id == houseId, ct);
        if (!houseExists)
            return ServiceResult<AvailabilityBlockDto>.NotFound();

        var validation = ValidateDates(dto.StartDate, dto.EndDate);
        if (validation is not null)
            return validation;

        var hasOverlap = await HasOverlapAsync(houseId, dto.StartDate, dto.EndDate, excludeBlockId: null, ct);
        if (hasOverlap)
            return ServiceResult<AvailabilityBlockDto>.Conflict("dates", "The specified dates overlap with an existing availability block.");

        var entity = new AvailabilityBlock
        {
            HouseId = houseId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            Source = AvailabilitySource.Manual,
            Note = dto.Note?.Trim()
        };

        db.AvailabilityBlocks.Add(entity);
        await db.SaveChangesAsync(ct);

        return ServiceResult<AvailabilityBlockDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult<AvailabilityBlockDto>> UpdateBlockAsync(
        Guid blockId, UpsertAvailabilityBlockDto dto, CancellationToken ct)
    {
        var entity = await db.AvailabilityBlocks.FirstOrDefaultAsync(b => b.Id == blockId, ct);
        if (entity is null)
            return ServiceResult<AvailabilityBlockDto>.NotFound();

        var validation = ValidateDates(dto.StartDate, dto.EndDate);
        if (validation is not null)
            return validation;

        var hasOverlap = await HasOverlapAsync(entity.HouseId, dto.StartDate, dto.EndDate, excludeBlockId: blockId, ct);
        if (hasOverlap)
            return ServiceResult<AvailabilityBlockDto>.Conflict("dates", "The specified dates overlap with an existing availability block.");

        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.Status = dto.Status;
        entity.Note = dto.Note?.Trim();

        await db.SaveChangesAsync(ct);

        return ServiceResult<AvailabilityBlockDto>.Success(ToDto(entity));
    }

    public async Task<ServiceResult> DeleteBlockAsync(Guid blockId, CancellationToken ct)
    {
        var entity = await db.AvailabilityBlocks.FirstOrDefaultAsync(b => b.Id == blockId, ct);
        if (entity is null)
            return ServiceResult.NotFound();

        db.AvailabilityBlocks.Remove(entity);
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    public async Task<bool> IsAvailableAsync(Guid houseId, DateOnly checkIn, DateOnly checkOut, CancellationToken ct)
    {
        var conflicting = await db.AvailabilityBlocks
            .AnyAsync(b =>
                b.HouseId == houseId
                && b.StartDate < checkOut
                && b.EndDate > checkIn
                && b.Status != AvailabilityStatus.Available, ct);

        return !conflicting;
    }

    private async Task<bool> HasOverlapAsync(Guid houseId, DateOnly start, DateOnly end, Guid? excludeBlockId, CancellationToken ct)
    {
        var query = db.AvailabilityBlocks
            .Where(b => b.HouseId == houseId && b.StartDate < end && b.EndDate > start);

        if (excludeBlockId.HasValue)
            query = query.Where(b => b.Id != excludeBlockId.Value);

        return await query.AnyAsync(ct);
    }

    private static ServiceResult<AvailabilityBlockDto>? ValidateDates(DateOnly start, DateOnly end)
    {
        if (end <= start)
            return ServiceResult<AvailabilityBlockDto>.Invalid("endDate", "End date must be after start date.");

        return null;
    }

    private static AvailabilityBlockDto ToDto(AvailabilityBlock b) => new(
        b.Id,
        b.HouseId,
        b.StartDate,
        b.EndDate,
        b.Status,
        b.Source,
        b.Note,
        b.CreatedAtUtc,
        b.CreatedBy);
}
