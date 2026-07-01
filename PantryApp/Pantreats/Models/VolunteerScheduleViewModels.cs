using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class VolunteerSchedulePageViewModel
    {
        public bool HasSchedule { get; set; }

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

        public bool HasPendingRequest { get; set; }
        public ScheduleChangeRequestSummaryViewModel? PendingRequest { get; set; }

        public List<VolunteerScheduleRosterRowViewModel> Roster { get; set; } = new();
    }

    public class ScheduleChangeRequestSummaryViewModel
    {
        public int ScheduleChangeRequestId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class VolunteerScheduleRosterRowViewModel
    {
        public string FullName { get; set; } = string.Empty;

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
    }

    public class RequestScheduleChangeViewModel
    {
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

        [Required(ErrorMessage = "Please tell us why you're requesting this change.")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ScheduleRequestAdminIndexViewModel
    {
        public List<ScheduleRequestSummaryViewModel> PendingRequests { get; set; } = new();
        public List<ScheduleRequestSummaryViewModel> ReviewedRequests { get; set; } = new();
    }

    public class ScheduleRequestSummaryViewModel
    {
        public int ScheduleChangeRequestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class ScheduleRequestDetailsViewModel
    {
        public int ScheduleChangeRequestId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }

        // What's currently on file, for side-by-side comparison
        public bool HasCurrentSchedule { get; set; }
        public bool CurrentMonMorning { get; set; }
        public bool CurrentMonAfternoon { get; set; }
        public bool CurrentTueMorning { get; set; }
        public bool CurrentTueAfternoon { get; set; }
        public bool CurrentWedMorning { get; set; }
        public bool CurrentWedAfternoon { get; set; }
        public bool CurrentThuMorning { get; set; }
        public bool CurrentThuAfternoon { get; set; }
        public bool CurrentFriMorning { get; set; }
        public bool CurrentFriAfternoon { get; set; }
        public bool CurrentSatMorning { get; set; }
        public bool CurrentSatAfternoon { get; set; }
        public bool CurrentSunMorning { get; set; }
        public bool CurrentSunAfternoon { get; set; }

        // What's being requested
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
    }
}