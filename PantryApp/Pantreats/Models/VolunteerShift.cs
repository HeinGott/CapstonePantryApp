using System.ComponentModel.DataAnnotations;

namespace Pantreats.Models
{
    public class VolunteerShift
    {
        public int VolunteerShiftId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

        public string Status { get; set; } = VolunteerShiftStatuses.Scheduled;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedByUserId { get; set; }

        public string? UpdatedByUserId { get; set; }
    }
}
