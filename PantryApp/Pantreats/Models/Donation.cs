using System;
using System.Collections.Generic;

namespace Pantreats.Models
{
    public class Donation
    {
        public int DonationId { get; set; }
        public int DonorId { get; set; }
        public DateTime DonationDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";

        public string? Address { get; set; }
        public string? Comment { get; set; }

        public List<DonationItem> DonationItems { get; set; } = new();

        public Donor? Donor { get; set; }
    }
}