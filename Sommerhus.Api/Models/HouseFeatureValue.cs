using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class HouseFeatureValue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }
    [Required] public Guid FeatureId { get; set; }

    public bool? ValueBool { get; set; }
    public int? ValueInt { get; set; }
    public decimal? ValueDecimal { get; set; }
    public string? ValueText { get; set; }

    public VacationHouse? House { get; set; }
    public Feature? Feature { get; set; }
}
