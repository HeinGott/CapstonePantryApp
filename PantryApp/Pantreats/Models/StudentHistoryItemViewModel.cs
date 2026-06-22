namespace Pantreats.Models
{
    public class StudentHistoryItemViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int PointsUsed { get; set; }
    }
}
