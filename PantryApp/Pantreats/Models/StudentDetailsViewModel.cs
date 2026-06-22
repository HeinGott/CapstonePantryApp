namespace Pantreats.Models
{
    public class StudentDetailsViewModel
    {
        public int ApplicationId { get; set; }
        public int StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Campus { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string StudentStatus { get; set; } = string.Empty;
        public bool? HasTransportation { get; set; }
        public string EmploymentStatus { get; set; } = string.Empty;
        public int EmployedHouseMembers { get; set; }
        public int HouseholdBabiesToddlers { get; set; }
        public int HouseholdChildren { get; set; }
        public int HouseholdTeens { get; set; }
        public int HouseholdAdults { get; set; }
        public int HouseholdTotal { get; set; }
        public bool HasSnap { get; set; }
        public bool HasWic { get; set; }
        public bool HasTanf { get; set; }
        public bool InterestedInSnap { get; set; }
        public bool InterestedInWic { get; set; }
        public bool InterestedInTanf { get; set; }
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
        public int TotalOrders { get; set; }
        public List<StudentHistoryItemViewModel> OrderHistory { get; set; } = new();
        public bool ShowOrderHistory { get; set; }

        public string DisplayPhoneNumber => FormatPhoneNumber(PhoneNumber);

        public string DisplayGender => FormatTitleCaseValue(Gender);

        public string DisplayLegalName => string.Join(" ", new[] { FirstName, MiddleName, LastName }
            .Where(namePart => !string.IsNullOrWhiteSpace(namePart)));

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

        private static string FormatTitleCaseValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Not provided";
            }

            if (value.Contains(' ') || value.Contains('-'))
            {
                return string.Join(" ",
                    value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
            }

            return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
        }
    }
}
