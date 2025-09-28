namespace Sommerhus.Contracts.Dtos.Admin.Houses;

// Minimal create DTO (udvid bare ved behov – klienten forventer blot typen eksisterer)
public record CreateHouseDto(
    string Title,
    Guid? CityId,
    string? Subtitle = null,
    string? Address = null,
    string? Description = null,
    string? Facilities = null);
