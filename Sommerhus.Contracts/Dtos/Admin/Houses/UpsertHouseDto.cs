using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Contracts.Dtos.Admin.Houses;

public sealed class UpsertHouseDto
{

    [Required(ErrorMessage = "Titel er påkrævet")]
    [MaxLength(140, ErrorMessage = "Titel må maks være 140 tegn")]
    public string Name { get; set; } = string.Empty;


    [Required(ErrorMessage = "By er påkrævet")]
    public Guid CityId { get; set; }


    [Required(ErrorMessage = "Adresse er påkrævet")]
    [MaxLength(200, ErrorMessage = "Adresse må maks være 200 tegn")]
    public string Address { get; set; } = string.Empty;


    [Required(ErrorMessage = "Beskrivelse er påkrævet")]
    public string Description { get; set; } = string.Empty;
    public List<Guid> AreaIds { get; set; } = new();
}
