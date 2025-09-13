// Sommerhus.Api/Data/Entities/Cities.cs
namespace Sommerhus.Api.Data;

public class City
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string? Slug { get; set; }

    // Ny: fri tekst/HTML til områdebeskrivelse
    public string? Text { get; set; }

    // Navigation til områdebilleder
    public List<CityImage> Images { get; set; } = new();
}
