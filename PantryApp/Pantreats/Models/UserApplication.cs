using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class UserApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        public string UserId { get; set; }

        public int StudentId { get; set; }

        public DateTime RegistrationDate { get; set; }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public DateTime? DOB { get; set; }

        public string PhoneNum { get; set; }

        public string Gender { get; set; }
        public string StudentStatus { get; set; }

        public byte HouseholdBabiesToddlers { get; set; }
        public byte HouseholdBabiesChildren { get; set; }
        public byte HouseholdTeens { get; set; }
        public byte HouseholdAdults { get; set; }

        public bool? HasTransportation { get; set; }

        public string EmploymentStatus { get; set; }

        public byte EmployedHouseMembers { get; set; }

        public bool HasSNAP { get; set; }
        public bool HasWIC { get; set; }
        public bool HasTANF { get; set; }

        public bool IsInterestedInSNAP { get; set; }
        public bool IsInterestedInWIC { get; set; }
        public bool IsInterestedInTANF { get; set; }

        public bool IsActive { get; set; }

        public string Campus { get; set; }

        public string ApplicationStatus { get; set; } = ApplicationStatuses.Pending;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedByUserId { get; set; }

        public string? ReviewNotes { get; set; }
    }
}
