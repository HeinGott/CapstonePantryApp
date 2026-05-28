namespace Pantreats.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public string UserId { get; set; } // Foreign key
        public string Email { get; set; }
        public string PhoneNum { get; set; }
        public DateTime OrderDate { get; set; }
        public int Total { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); //nav property that links to OrderItems,
        //easier to access the items in an order when we have the order object
    }
}
