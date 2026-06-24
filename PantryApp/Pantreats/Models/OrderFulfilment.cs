namespace Pantreats.Models
{
    public class OrderFulfilment
    {
        public const string LegacyStatusWaitingPickup = "Waiting Pickup";
        public const string LegacyStatusFulfilled = "Fulfilled";
        public const string StatusOrderPlaced = "Order placed";
        public const string StatusReadyForPickup = "Order ready for pickup";
        public const string StatusCompleted = "Completed";

        public int Id { get; set; }
        public int OrderId { get; set; } // Foreign key
        public Order? Order { get; set; }
        public DateTime FulfilmentDate { get; set; } // Tracks when the order was marked ready for pickup.
        public DateTime? DateReceived { get; set; } // Tracks when the student received the completed order.
        public string OrderStatus { get; set; } = StatusOrderPlaced;

        public static string NormalizeStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return StatusOrderPlaced;
            }

            return status switch
            {
                LegacyStatusWaitingPickup => StatusReadyForPickup,
                LegacyStatusFulfilled => StatusCompleted,
                "Submitted" => StatusOrderPlaced,
                _ => status
            };
        }
    }
}
