using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class HouseFeatureValue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }
    [Required] public Guid FeatureId { get; set; }

    [Required]
    public string RawValue { get; set; } = "";

    public VacationHouse? House { get; set; }
    public Feature? Feature { get; set; }
}
