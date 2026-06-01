namespace Pantreats.Models
{
    public class OrderFulfilmentViewModel
    {
        public List<OrderFulfilment> OrderFulfilments { get; set; } = new List<OrderFulfilment>();
        public int TotalOrders { get; set; }
        public int WaitingPickupOrders { get; set; }
        public int FulfilledOrders { get; set; }
        public string? SearchText { get; set; }
        public string? StatusFilter { get; set; }
    }
}
