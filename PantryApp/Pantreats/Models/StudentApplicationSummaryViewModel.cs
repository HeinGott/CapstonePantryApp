namespace Pantreats.Models
{
    public class StudentApplicationSummaryViewModel
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }
}
