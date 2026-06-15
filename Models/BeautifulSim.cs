using System.ComponentModel.DataAnnotations;

namespace ViettalAPI.Models
{
    public class BeautifulSim
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Carrier { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        public int Price { get; set; }

        [Required]
        [MaxLength(500)]
        public string Meaning { get; set; } = string.Empty;

        [Required]
        public SimStatus Status { get; set; } = SimStatus.Available;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
    }
}
