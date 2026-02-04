using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class Feature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = "";

    [Required, MaxLength(60)]
    public string Key { get; set; } = "";

    [Required]
    public FeatureValueType ValueType { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    [MaxLength(300)]
    public string? IconUrl { get; set; }

    public int SortOrder { get; set; } = 0;
}
