namespace Sommerhus.Domain.Models;

public class FavoriteHouse
{
    public string UserId { get; set; } = "";
    public Guid HouseId { get; set; }
    public VacationHouse House { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
