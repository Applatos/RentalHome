using System;
using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Mvc.Models.Admin;

public sealed class AreaFormInput
{
    [Required(ErrorMessage = "Navn er påkrævet")]
    [Display(Name = "Navn")]
    public string? Name { get; set; }

    [Display(Name = "By")]
    public Guid? CityId { get; set; }

    [Display(Name = "Beskrivelse")]
    public string? Description { get; set; }
}
