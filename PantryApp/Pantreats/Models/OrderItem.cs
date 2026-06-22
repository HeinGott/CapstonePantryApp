
namespace Pantreats.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; } // Foreign key to Orders
        public int? InventoryItemId { get; set; } // Foreign key to Inventory
        public string? InventoryUPC { get; set; } // Foreign key to Inventory
        public string ItemName { get; set; }
        public string Category { get; set; }
        public int OrderQuantity { get; set; }
        public int Points { get; set; }

        public Inventory Inventory { get; set; }//nav property to access inventory details of the item in the order

    }
}
