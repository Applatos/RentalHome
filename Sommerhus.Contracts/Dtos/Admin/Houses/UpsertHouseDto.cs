using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public sealed class UpsertHouseDto
{
    [Required]
    public string Name { get; set; }
    public Guid CityId { get; set; }
    public Guid? AreaId { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
}
