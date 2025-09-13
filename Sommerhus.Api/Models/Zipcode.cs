using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Models;

public class ZipCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(10)]
    public string Zip { get; set; } = "";

    [Required, MaxLength(80)]
    public string City { get; set; } = "";
}
