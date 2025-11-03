using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class Area
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(50)]
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ICollection<City> Cities { get; set; } = new List<City>();
    public ICollection<VacationHouse> Houses { get; set; } = new List<VacationHouse>();
    public List<AreaImage> AreaImages { get; set; } = new();
}
