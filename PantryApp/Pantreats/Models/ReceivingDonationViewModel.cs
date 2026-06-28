namespace Pantreats.Models
{
    public class ReceivingDonationViewModel
    {
        public string DonorMode { get; set; } = "existing";
        public int? SelectedDonorId { get; set; }

        public string? ManualName { get; set; }
        public string? ManualPhoneNumber { get; set; }
        public string? ManualEmail { get; set; }
        public string? ManualAddress { get; set; }

        public string? DonationAddress { get; set; }
        public string? Comment { get; set; }

        public List<DonationItemInput> Items { get; set; } = new();
        public List<AdminDonorOptionViewModel> RegisteredDonors { get; set; } = new();
    }
}
