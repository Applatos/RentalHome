using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class AreaImage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid AreaId { get; set; }

    [Required, MaxLength(200)]
    public string FileName { get; set; } = "";

    public int SortOrder { get; set; }

    public Area? Area { get; set; }
}
