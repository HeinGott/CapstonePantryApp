using System.ComponentModel.DataAnnotations;
public class UserApplicationViewModel
{
    //basic info
    [Required(ErrorMessage = "Student ID is required")]
    [Range(0, 9999999, ErrorMessage = "Student ID must be a valid number")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; }

    [Required(ErrorMessage = "Middle name is required")]
    public string MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    public DateTime? DOB { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    public string PhoneNum { get; set; }

    //status and gender
    [Required(ErrorMessage = "Gender is required")]
    public string Gender { get; set; }

    [Required(ErrorMessage = "Student status is required")]
    public string StudentStatus { get; set; }

    //ethnicity
    [Required(ErrorMessage = "Ethnicity selection is required")]
    public string Ethnicity { get; set; }

    //campus
    [Required(ErrorMessage = "Campus selection is required")]
    public string Campus { get; set; }

    //household members
    public byte HouseholdBabiesToddlers { get; set; }
    public byte HouseholdBabiesChildren { get; set; }
    public byte HouseholdTeens { get; set; }
    public byte HouseholdAdults { get; set; }
    
    //transportation
    public bool HasTransportation { get; set; }

    //employment
    [Required(ErrorMessage = "Employment status is required")]
    public string EmploymentStatus { get; set; }
    public byte EmployedHouseMembers { get; set; }

    //benefits
    public bool HasSNAP { get; set; }
    public bool HasWIC { get; set; }
    public bool HasTANF { get; set; }

    public bool IsInterestedInSNAP { get; set; }
    public bool IsInterestedInWIC { get; set; }
    public bool IsInterestedInTANF { get; set; }
}
