using System.ComponentModel.DataAnnotations;

namespace Pantreats.Models
{
    public class VolunteerApplicationViewModel
    {
        // Basic Info
        [Required(ErrorMessage = "First name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string PhoneNum { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please select your year")]
        public string Year { get; set; }

        // Volunteer Experience
        public bool HasVolunteeredBefore { get; set; }
        public string? PreviousCapacity { get; set; }

        [Required(ErrorMessage = "Please tell us why you want to volunteer")]
        public string ReasonForVolunteering { get; set; }

        // Frequency
        [Required(ErrorMessage = "Please select how often you'd like to volunteer")]
        public string VolunteerFrequency { get; set; }
        public string? OtherFrequency { get; set; }

        // Weekly Availability
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
