namespace MVC_Sommerhus.Models
{
    public class Repository
    {
        public List<House> Houses { get; set; }

        public Repository()
        {
            Houses = new List<House>
            {
            new House
                {
                    Id = 1,
                    Name = "Almosetoften",
                    Description = "A cozy house in Ho",
                    Capacity = 9,
                    ImageUrl = new List<string>
                    {
                        "/images/houses/house1/1.jpg",
                        "/images/houses/house1/2.jpg",
                        "/images/houses/house1/3.jpg"
                    },
                    Specs = new List<string>
                    {
                        "/images/specs/spec1.png",
                        "/images/specs/spec2.png",
                        "/images/specs/spec3.png"
                    }
                },
            new House
                {
                Id = 2,
                Name = "Strandvejen",
                Description = "A beautiful house in Blåvand",
                Capacity = 12,
                ImageUrl = new List<string>
                    {
                        "/images/houses/house2/1.jpg",
                        "/images/houses/house2/2.jpg",
                        "/images/houses/house2/3.jpg"
                    },
                Specs = new List<string>
                    {
                    "/images/specs/spec4.png",
                    "/images/specs/spec5.png",
                    "/images/specs/spec6.png"
                    }
                }
            };
        }
    }
}
