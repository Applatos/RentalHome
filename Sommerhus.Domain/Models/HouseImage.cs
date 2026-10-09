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

    /// <summary>Position in the gallery. Uploads are numbered in the order they arrive.</summary>
    public int SortOrder { get; set; }

    public VacationHouse? House { get; set; }
}
