using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sommerhus.Domain.Models;

namespace Sommerhus.Core.Data;

public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly HashSet<string> AuditedEntityTypes = new(StringComparer.Ordinal)
    {
        nameof(VacationHouse),
        nameof(Area),
        nameof(Feature),
        nameof(HouseGroup),
        "PricePlan",
        "SeasonPrice"
    };

    private List<PendingAuditEntry>? pendingEntries;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            BeforeSave(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            BeforeSave(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
        {
            AfterSave(eventData.Context);
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AfterSave(eventData.Context);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void BeforeSave(DbContext context)
    {
        var userName = GetCurrentUser();
        var now = DateTime.UtcNow;

        pendingEntries = new List<PendingAuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Layer 1: Auto-populate IAuditable timestamps
            if (entry.Entity is IAuditable auditable)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        auditable.CreatedAtUtc = now;
                        auditable.CreatedBy ??= userName;
                        break;

                    case EntityState.Modified:
                        auditable.UpdatedAtUtc = now;
                        auditable.UpdatedBy = userName;
                        break;
                }
            }

            // Layer 2: Build change log for audited entities
            var entityTypeName = entry.Entity.GetType().Name;
            if (!AuditedEntityTypes.Contains(entityTypeName))
            {
                continue;
            }

            var entityId = GetEntityId(entry);
            if (entityId is null)
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    pendingEntries.Add(new PendingAuditEntry
                    {
                        EntityType = entityTypeName,
                        EntityId = entityId,
                        Action = AuditAction.Created,
                        ChangedBy = userName,
                        ChangedAtUtc = now,
                        Changes = SerializeCurrentValues(entry)
                    });
                    break;

                case EntityState.Modified:
                    pendingEntries.Add(new PendingAuditEntry
                    {
                        EntityType = entityTypeName,
                        EntityId = entityId,
                        Action = AuditAction.Updated,
                        ChangedBy = userName,
                        ChangedAtUtc = now,
                        Changes = SerializeChanges(entry)
                    });
                    break;

                case EntityState.Deleted:
                    pendingEntries.Add(new PendingAuditEntry
                    {
                        EntityType = entityTypeName,
                        EntityId = entityId,
                        Action = AuditAction.Deleted,
                        ChangedBy = userName,
                        ChangedAtUtc = now,
                        Changes = SerializeOriginalValues(entry)
                    });
                    break;
            }
        }
    }

    private void AfterSave(DbContext context)
    {
        if (pendingEntries is null || pendingEntries.Count == 0)
        {
            return;
        }

        // For Added entities, the Id may have been generated after SaveChanges
        // Re-resolve Ids for Created entries if needed
        var auditEntries = pendingEntries.Select(p => new AuditEntry
        {
            EntityType = p.EntityType,
            EntityId = p.EntityId,
            Action = p.Action,
            ChangedBy = p.ChangedBy,
            ChangedAtUtc = p.ChangedAtUtc,
            Changes = p.Changes
        }).ToList();

        context.Set<AuditEntry>().AddRange(auditEntries);

        pendingEntries = null;

        // Save audit entries in a separate call to avoid infinite recursion
        context.SaveChanges();
    }

    private string GetCurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirstValue(ClaimTypes.Name)
                ?? user.FindFirstValue(ClaimTypes.Email)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";
        }

        return "system";
    }

    private static string? GetEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (pk is null)
        {
            return null;
        }

        var value = pk.CurrentValue ?? pk.OriginalValue;
        return value?.ToString();
    }

    private static string? SerializeChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties.Where(p => p.IsModified))
        {
            var name = prop.Metadata.Name;
            changes[name] = new
            {
                from = prop.OriginalValue,
                to = prop.CurrentValue
            };
        }

        return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, JsonOptions);
    }

    private static string? SerializeCurrentValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
        {
            values[prop.Metadata.Name] = prop.CurrentValue;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private static string? SerializeOriginalValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
        {
            values[prop.Metadata.Name] = prop.OriginalValue;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private sealed class PendingAuditEntry
    {
        public string EntityType { get; set; } = "";
        public string EntityId { get; set; } = "";
        public AuditAction Action { get; set; }
        public string ChangedBy { get; set; } = "";
        public DateTime ChangedAtUtc { get; set; }
        public string? Changes { get; set; }
    }
}
