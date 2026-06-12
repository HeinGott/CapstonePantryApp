namespace Pantreats.Models
{
    public class DonorDashboardViewModel
    {
        public string DonorName { get; set; } = "";
        public int TotalDonations { get; set; }
        public double TotalItemsDonated { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public List<Donation> Donations { get; set; } = new List<Donation>();
    }
}