namespace Pantreats.Models
{
    public class DonationItem
    {
        public int DonationItemId { get; set; }
        public int DonationId { get; set; }
        public string ItemName { get; set; } = "";
        public int Quantity { get; set; }
        public Donation? Donation { get; set; }
    }
}
