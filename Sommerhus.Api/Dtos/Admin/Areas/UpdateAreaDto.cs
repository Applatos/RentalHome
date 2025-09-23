using System.ComponentModel.DataAnnotations;

namespace Sommerhus.Api.Dtos.Admin.Areas;

public class UpdateAreaDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid? CityId { get; set; }
    public List<string> Images { get; set; } = new();
}