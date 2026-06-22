using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class VolunteerApplication
    {
        // Volunteer Application Id/ primary key
        public int VolunteerApplicationId { get; set; }
        // User Id
        public string UserId { get; set; }

        // Basic Info
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNum { get; set; }
        public string Email { get; set; }
        public string Year { get; set; }

        // Volunteer Experience
        public bool HasVolunteeredBefore { get; set; }
        public string? PreviousCapacity { get; set; }
        public string ReasonForVolunteering { get; set; }

        // Frequency
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

        // Auto-stamp when the application was submitted
        public DateTime SubmittedDate { get; set; } = DateTime.Now;
    }
}
