using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class Area : IAuditable
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(50)]
    public string Name { get; set; } = "";

    [StringLength(100)]
    public string? NameEn { get; set; }

    public string? Description { get; set; }

    public EntityStatus Status { get; set; } = EntityStatus.Draft;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }

    public ICollection<City> Cities { get; set; } = new List<City>();
    public ICollection<VacationHouse> Houses { get; set; } = new List<VacationHouse>();
    public List<AreaImage> AreaImages { get; set; } = new();
}
