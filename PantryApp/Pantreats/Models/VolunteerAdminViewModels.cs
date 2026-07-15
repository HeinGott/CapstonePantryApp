namespace Pantreats.Models
{
    public class VolunteerAdminIndexViewModel
    {
        public int TotalVolunteers { get; set; }
        public int PendingScheduleRequests { get; set; }
        public int UpcomingShiftsThisWeek { get; set; }
        public int TodaysShifts { get; set; }
        public List<VolunteerApplicationSummaryViewModel> PendingApplications { get; set; } = new();
        public List<VolunteerApplicationSummaryViewModel> ApprovedVolunteers { get; set; } = new();
        public List<VolunteerShiftSummaryViewModel> UpcomingShifts { get; set; } = new();
    }

    public class VolunteerApplicationSummaryViewModel
    {
        public int VolunteerApplicationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    public class VolunteerDetailsViewModel
    {
        public int VolunteerApplicationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public string ReasonForVolunteering { get; set; } = string.Empty;

        public string VolunteerFrequency { get; set; } = string.Empty;
        public string? OtherFrequency { get; set; }

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

        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public DateTime? AvailabilityLastUpdated { get; set; }
        public List<VolunteerShiftSummaryViewModel> UpcomingShifts { get; set; } = new();

        public string DisplayPhoneNumber => FormatPhoneNumber(PhoneNumber);

        private static string FormatPhoneNumber(string? phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return "Not provided";
            }

            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digits.Length == 11 && digits.StartsWith("1"))
            {
                digits = digits[1..];
            }

            if (digits.Length == 10)
            {
                return $"({digits[..3]}) - {digits[3..6]} - {digits[6..]}";
            }

            return phoneNumber;
        }
    }
    public class VolunteerApplicationStatusViewModel
    {
        public int VolunteerApplicationId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public bool CanEditApplication { get; set; }
    }

    public class VolunteerShiftSummaryViewModel
    {
        public int VolunteerShiftId { get; set; }
        public int VolunteerApplicationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime ShiftDate { get; set; }
        public string TimeLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool OutsideAvailability { get; set; }
        public string? Notes { get; set; }
    }
}
