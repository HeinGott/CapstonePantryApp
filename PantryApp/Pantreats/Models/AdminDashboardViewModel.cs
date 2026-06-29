namespace Pantreats.Models
{
    public class AdminDashboardViewModel
    {
        public int NewOrders { get; set; }
        public int PendingApplications { get; set; }
        public int PendingVolunteerApplications { get; set; }
        public int NewDonations { get; set; }
        public List<AdminDashboardOrderViewModel> RecentOrders { get; set; } = new();
        public List<AdminDashboardApplicationViewModel> RecentApplications { get; set; } = new();
        public List<AdminDashboardDonationViewModel> RecentDonations { get; set; } = new();
    }

    public class AdminDashboardOrderViewModel
    {
        public int OrderId { get; set; }
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int PointsUsed { get; set; }
    }

    public class AdminDashboardApplicationViewModel
    {
        public int ApplicationId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class AdminDashboardDonationViewModel
    {
        public int DonationId { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TotalUnits { get; set; }
    }
}
