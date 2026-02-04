using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Domain.Models;

public class HouseImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required] public Guid HouseId { get; set; }

    [Required, MaxLength(300)]
    public string FileName { get; set; } = "";

    [MaxLength(300)]
    public string? Alt { get; set; }

    [Required]
    public ImageKind Kind { get; set; } = ImageKind.Gallery;

    public VacationHouse? House { get; set; }
}
