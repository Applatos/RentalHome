using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonCode
{
    [Key]
    public string Code { get; set; } = "A";
    public string Name { get; set; } = "";
    public string Color { get; set; } = "#6C757D";
    public int SortOrder { get; set; }
}
