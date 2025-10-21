namespace Sommerhus.Api.Models
{
    public class HouseAreas
    {
        public Guid HouseId { get; set; }
        public Guid AreaId { get; set; }
        public VacationHouse House { get; set; } = null!;
        public Area Area { get; set; } = null!;

        public int isPrimary { get; set; }
    }
}
