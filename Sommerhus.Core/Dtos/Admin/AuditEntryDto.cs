namespace Sommerhus.Core.Dtos.Admin;

public sealed record AuditEntryDto(
    long Id,
    string EntityType,
    string EntityId,
    string Action,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string? Changes);
