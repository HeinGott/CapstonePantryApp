using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class ScheduleChangeRequest
    {
        public int ScheduleChangeRequestId { get; set; }
        public string UserId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool MonMorning { get; set; }
        public bool MonAfternoon { get; set; }
        public bool TueMorning { get; set; }
        public bool TueAfternoon { get; set; }
        public bool WedMorning { get; set; }
        public bool WedAfternoon { get; set; }
        public bool ThuMorning { get; set; }
        public bool ThuAfternoon { get; set; }
        public bool FriMorning { get; set; }
        public bool FriAfternoon { get; set; }
        public bool SatMorning { get; set; }
        public bool SatAfternoon { get; set; }
        public bool SunMorning { get; set; }
        public bool SunAfternoon { get; set; }

        public string Reason { get; set; }

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public string RequestStatus { get; set; } = ApplicationStatuses.Pending;
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewedByUserId { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
