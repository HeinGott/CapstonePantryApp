namespace Pantreats.Models
{
    public class OrderFulfilment
    {
        public int Id { get; set; }
        public int OrderId { get; set; } // Foreign key
        public Order? Order { get; set; }
        public DateTime FulfilmentDate { get; set; } //For orders packed date
        public DateTime? DateReceived { get; set; } //For orders not received yet
        public string OrderStatus { get; set; } = "Waiting Pickup";
    }
}
