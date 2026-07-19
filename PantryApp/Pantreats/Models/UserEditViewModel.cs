using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pantreats.Models
{
    public class UserEditViewModel
    {
        public string UserId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty;

        public int? StudentApplicationId { get; set; }
        public int? VolunteerApplicationId { get; set; }
        public int? DonorId { get; set; }

        public bool HasStudentApplication => StudentApplicationId.HasValue;
        public bool HasVolunteerApplication => VolunteerApplicationId.HasValue;
        public bool HasDonorProfile => DonorId.HasValue;

        public DateTime? StudentSubmittedAt { get; set; }
        public DateTime? StudentReviewedAt { get; set; }
        public string? StudentApplicationStatus { get; set; }
        public string? StudentReviewNotes { get; set; }
        public bool StudentApplicationIsActive { get; set; }
        public bool StudentApplicationIsVolunteer { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Monthly point balance cannot be negative.")]
        public int? MonthlyPointBalance { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Current point balance cannot be negative.")]
        public int? CurrentPointBalance { get; set; }
        public DateTime? LastPointResetAt { get; set; }
        public int? StudentNumber { get; set; }
        public string StudentFirstName { get; set; } = string.Empty;
        public string? StudentMiddleName { get; set; }
        public string StudentLastName { get; set; } = string.Empty;
        public DateTime? StudentDateOfBirth { get; set; }
        public string StudentApplicationPhoneNumber { get; set; } = string.Empty;
        public string StudentGender { get; set; } = string.Empty;
        public string StudentStatus { get; set; } = string.Empty;
        public string StudentCampus { get; set; } = string.Empty;
        public byte HouseholdBabiesToddlers { get; set; }
        public byte HouseholdChildren { get; set; }
        public byte HouseholdTeens { get; set; }
        public byte HouseholdAdults { get; set; }
        public bool? HasTransportation { get; set; }
        public string EmploymentStatus { get; set; } = string.Empty;
        public byte EmployedHouseMembers { get; set; }
        public bool HasSnap { get; set; }
        public bool HasWic { get; set; }
        public bool HasTanf { get; set; }
        public bool InterestedInSnap { get; set; }
        public bool InterestedInWic { get; set; }
        public bool InterestedInTanf { get; set; }

        public DateTime? VolunteerSubmittedAt { get; set; }
        public DateTime? VolunteerReviewedAt { get; set; }
        public string? VolunteerApplicationStatus { get; set; }
        public string? VolunteerReviewNotes { get; set; }
        public string VolunteerFirstName { get; set; } = string.Empty;
        public string VolunteerLastName { get; set; } = string.Empty;
        public string VolunteerEmail { get; set; } = string.Empty;
        public string VolunteerPhoneNumber { get; set; } = string.Empty;
        public string VolunteerYear { get; set; } = string.Empty;
        public bool HasVolunteeredBefore { get; set; }
        public string? PreviousCapacity { get; set; }
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

        public string DonorName { get; set; } = string.Empty;
        public string? DonorEmail { get; set; }
        public string? DonorPhoneNumber { get; set; }
        public string? DonorAddress { get; set; }

        public List<SelectListItem> AvailableRoles { get; set; } = new();

        public int HouseholdTotal => HouseholdBabiesToddlers + HouseholdChildren + HouseholdTeens + HouseholdAdults;
    }
}
