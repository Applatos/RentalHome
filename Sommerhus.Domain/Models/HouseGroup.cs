using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class HouseGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(100)] public string Name { get; set; } = "";
}
