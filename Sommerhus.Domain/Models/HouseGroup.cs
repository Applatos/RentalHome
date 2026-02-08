using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class HouseGroup : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string Name { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }
}
