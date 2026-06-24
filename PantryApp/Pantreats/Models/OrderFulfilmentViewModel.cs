namespace Pantreats.Models
{
    public class OrderFulfilmentViewModel
    {
        public List<OrderFulfilment> OrderFulfilments { get; set; } = new List<OrderFulfilment>();
        public int TotalOrders { get; set; }
        public int OrderPlacedCount { get; set; }
        public int ReadyForPickupCount { get; set; }
        public int CompletedCount { get; set; }
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
    }
}
