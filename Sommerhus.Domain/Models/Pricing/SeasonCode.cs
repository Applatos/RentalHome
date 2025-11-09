using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonCode
{
    [Key]
    public string Code { get; set; } = "A";
    public string Name { get; set; } = "Højsæson";
    public string Color { get; set; }
    public int SortOrder { get; set; }
}
