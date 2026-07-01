using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class VolunteerSchedule
    {
        public int VolunteerScheduleId { get; set; }
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

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
