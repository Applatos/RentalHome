using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Core.Dtos.Admin;

public sealed class UpsertHouseDto
{

    [Required(ErrorMessage = "Titel er p�kr�vet")]
    [MaxLength(140, ErrorMessage = "Titel m� maks v�re 140 tegn")]
    public string Name { get; set; } = string.Empty;


    [Required(ErrorMessage = "By er p�kr�vet")]
    public Guid CityId { get; set; }


    [Required(ErrorMessage = "Adresse er p�kr�vet")]
    [MaxLength(200, ErrorMessage = "Adresse m� maks v�re 200 tegn")]
    public string Address { get; set; } = string.Empty;


    [Required(ErrorMessage = "Beskrivelse er p�kr�vet")]
    public string Description { get; set; } = string.Empty;
    public List<Guid> AreaIds { get; set; } = new();
}
