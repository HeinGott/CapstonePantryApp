namespace Pantreats.Models
{
    public class StudentAdminIndexViewModel
    {
        public List<StudentApplicationSummaryViewModel> PendingApplications { get; set; } = new();
        public List<StudentApplicationSummaryViewModel> ApprovedStudents { get; set; } = new();
    }
}
