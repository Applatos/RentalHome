using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class Feature : IAuditable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(60)]
    public string Key { get; set; } = "";

    [Required]
    public FeatureValueType ValueType { get; set; }

    public FeatureCategory Category { get; set; } = FeatureCategory.Other;

    public bool IsSearchable { get; set; } = true;

    [MaxLength(500)]
    public string? Options { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    [MaxLength(300)]
    public string? IconUrl { get; set; }

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }
}
