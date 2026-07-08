using System.ComponentModel.DataAnnotations;

namespace Pantreats.Models
{
    public class VolunteerSchedulerPageViewModel
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public List<VolunteerSchedulerDayViewModel> Days { get; set; } = new();
        public List<VolunteerSchedulerVolunteerViewModel> Volunteers { get; set; } = new();
        public VolunteerShiftFormViewModel ShiftForm { get; set; } = new();
        public bool IsEditingShift { get; set; }
    }

    public class VolunteerSchedulerDayViewModel
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public List<VolunteerSchedulerShiftCardViewModel> Shifts { get; set; } = new();
    }

    public class VolunteerSchedulerVolunteerViewModel
    {
        public int VolunteerApplicationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string AvailabilitySummary { get; set; } = string.Empty;
        public List<VolunteerSchedulerAvailabilityRowViewModel> AvailabilityRows { get; set; } = new();
        public bool HasAvailabilityOnSelectedDay { get; set; }
        public DateTime? NextShiftDate { get; set; }
        public string? NextShiftTimeLabel { get; set; }
    }

    public class VolunteerSchedulerAvailabilityRowViewModel
    {
        public string DayLabel { get; set; } = string.Empty;
        public bool Morning { get; set; }
        public bool Afternoon { get; set; }
    }

    public class VolunteerSchedulerShiftCardViewModel
    {
        public int VolunteerShiftId { get; set; }
        public int VolunteerApplicationId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool OutsideAvailability { get; set; }
        public string? Notes { get; set; }
        public int StartMinutes { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class VolunteerShiftFormViewModel
    {
        public int VolunteerShiftId { get; set; }

        [Required(ErrorMessage = "Please choose a volunteer.")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose a shift date.")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        [Required(ErrorMessage = "Please choose a start time.")]
        [DataType(DataType.Time)]
        public string StartTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose an end time.")]
        [DataType(DataType.Time)]
        public string EndTime { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime WeekStart { get; set; }
    }

    public class VolunteerAssignedShiftViewModel
    {
        public int VolunteerShiftId { get; set; }
        public DateTime ShiftDate { get; set; }
        public string TimeLabel { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool OutsideAvailability { get; set; }
    }

    public class VolunteerSchedulerShiftResponseViewModel
    {
        public int VolunteerShiftId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string ShiftDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool OutsideAvailability { get; set; }
        public int StartMinutes { get; set; }
        public int DurationMinutes { get; set; }
    }
}
