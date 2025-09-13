using System.ComponentModel.DataAnnotations;

namespace MVC_Sommerhus.Models
{
    public class House
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public int Capacity { get; set; }

        [StringLength(255)]
        public List<string> ImageUrl { get; set; } = new List<string>();

        public List<string> Specs { get; set; } = new List<string>();
    }
}