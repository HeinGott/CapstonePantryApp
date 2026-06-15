namespace Pantreats.Models
{
    public class Donation
    {
        public int DonationId { get; set; }
        public int DonorId { get; set; }
        public  DateTime DonationDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
        public List<DonationItem> DonationItems { get; set; } = new();

        public Donor? Donor { get; set; }
    }
}
