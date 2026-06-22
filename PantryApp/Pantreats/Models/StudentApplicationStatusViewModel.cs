namespace Pantreats.Models
{
    public class StudentApplicationStatusViewModel
    {
        public int ApplicationId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public bool CanEditApplication { get; set; }
    }
}
