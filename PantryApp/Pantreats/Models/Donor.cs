using System.ComponentModel.DataAnnotations;

namespace Pantreats.Models
{
    public class Donor
    {
        public int DonorID { get; set; }
        public string? UserId { get; set; }

        [Required]
        public string Name { get; set; } = "";

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }
        public string? Address { get; set; }
        public List<Donation> Donations { get; set; } = new();

    }
}