using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public enum AuditAction
{
    Created = 0,
    Updated = 1,
    Deleted = 2
}

public class AuditEntry
{
    public long Id { get; set; }

    [Required, MaxLength(100)]
    public string EntityType { get; set; } = "";

    [Required, MaxLength(64)]
    public string EntityId { get; set; } = "";

    public AuditAction Action { get; set; }

    [Required, MaxLength(256)]
    public string ChangedBy { get; set; } = "";

    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;

    public string? Changes { get; set; }
}
