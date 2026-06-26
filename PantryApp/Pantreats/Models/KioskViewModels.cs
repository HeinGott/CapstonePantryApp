namespace Pantreats.Models
{
    public class KioskStudentLookupRequest
    {
        public string StudentId { get; set; } = string.Empty;
    }

    public class KioskItemLookupRequest
    {
        public string UPC { get; set; } = string.Empty;
    }

    public class KioskCheckoutRequest
    {
        public string StudentId { get; set; } = string.Empty;
        public List<string> UPCs { get; set; } = new();
    }
}
