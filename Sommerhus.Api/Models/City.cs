namespace Sommerhus.Api.Models;

public class City
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Zip { get; set; } = "";
    public List<VacationHouse> Houses { get; set; } = new();
}
