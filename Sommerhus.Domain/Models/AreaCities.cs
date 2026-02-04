namespace Sommerhus.Domain.Models;
public class AreaCities
{
    public Guid AreaId { get; set; } 
    public Guid CityId { get; set; }
    public Area Area { get; set; } = null!;
    public City City { get; set; } = null!;
}

