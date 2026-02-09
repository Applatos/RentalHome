using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonCalendar : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Name { get; set; } = "";

    public int? Year { get; set; }

    public bool IsTemplate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }

    public ICollection<SeasonSpan> Spans { get; set; } = new List<SeasonSpan>();
}
