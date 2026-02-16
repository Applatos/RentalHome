using Microsoft.EntityFrameworkCore;
using Sommerhus.Core.Common;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Services.Admin.Lifecycle;

public sealed class EntityLifecycleService(AppDbContext db, ISearchIndexer searchIndexer) : IEntityLifecycleService
{

    public async Task<ServiceResult> TransitionHouseAsync(Guid houseId, EntityStatus target, CancellationToken ct)
    {
        var house = await db.Houses
            .Include(h => h.Images)
            .FirstOrDefaultAsync(h => h.Id == houseId, ct);

        if (house is null)
            return ServiceResult.NotFound();

        var validationResult = ValidateHouseTransition(house, target);
        if (!validationResult.IsSuccess)
            return validationResult;

        ApplyHouseTransition(house, target);
        await db.SaveChangesAsync(ct);
        await searchIndexer.UpdateHouseAsync(houseId, ct);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> TransitionAreaAsync(Guid areaId, EntityStatus target, CancellationToken ct)
    {
        var area = await db.Areas.FirstOrDefaultAsync(a => a.Id == areaId, ct);

        if (area is null)
            return ServiceResult.NotFound();

        var validationResult = ValidateAreaTransition(area, target);
        if (!validationResult.IsSuccess)
            return validationResult;

        area.Status = target;
        await db.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }

    private static ServiceResult ValidateHouseTransition(VacationHouse house, EntityStatus target)
    {
        if (house.Status == target)
            return ServiceResult.Invalid("Status", $"House is already {target}.");

        if (!IsTransitionAllowed(house.Status, target))
            return ServiceResult.Invalid("Status", $"Transition from {house.Status} to {target} is not allowed.");

        if (target == EntityStatus.Published)
            return ValidatePublishPreconditions(house);

        return ServiceResult.Success();
    }

    private static ServiceResult ValidateAreaTransition(Area area, EntityStatus target)
    {
        if (area.Status == target)
            return ServiceResult.Invalid("Status", $"Area is already {target}.");

        if (!IsTransitionAllowed(area.Status, target))
            return ServiceResult.Invalid("Status", $"Transition from {area.Status} to {target} is not allowed.");

        return ServiceResult.Success();
    }

    private static bool IsTransitionAllowed(EntityStatus current, EntityStatus target) =>
        (current, target) switch
        {
            (EntityStatus.Draft, EntityStatus.Published) => true,
            (EntityStatus.Published, EntityStatus.Archived) => true,
            (EntityStatus.Published, EntityStatus.Draft) => true,
            (EntityStatus.Archived, EntityStatus.Draft) => true,
            _ => false
        };

    private static ServiceResult ValidatePublishPreconditions(VacationHouse house)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(house.Title))
            errors["Title"] = new[] { "Title is required to publish." };

        if (house.CityId == Guid.Empty)
            errors["CityId"] = new[] { "City is required to publish." };

        if (house.Images.Count == 0)
            errors["Images"] = new[] { "At least one image is required to publish." };

        return errors.Count > 0
            ? ServiceResult.Invalid(errors)
            : ServiceResult.Success();
    }

    private static void ApplyHouseTransition(VacationHouse house, EntityStatus target)
    {
        var now = DateTime.UtcNow;

        house.Status = target;

        switch (target)
        {
            case EntityStatus.Published:
                house.PublishedAtUtc ??= now;
                house.ArchivedAtUtc = null;
                break;
            case EntityStatus.Archived:
                house.ArchivedAtUtc = now;
                break;
            case EntityStatus.Draft:
                house.ArchivedAtUtc = null;
                break;
        }
    }
}
