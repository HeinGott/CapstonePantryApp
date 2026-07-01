namespace Pantreats.Models
{
    public class InventoryReportViewModel
    {
        public int TotalInventoryItems { get; set; }
        public int TotalInventoryQuantity { get; set; }
        public int LowStockCount { get; set; }

        public List<InventoryCategoryReportItem> CategoryTotals { get; set; } = new List<InventoryCategoryReportItem>();
        public List<LowStockReportItem> LowStockItems { get; set; } = new List<LowStockReportItem>();
        public List<InventoryFullReportItem> FullInventory { get; set; } = new List<InventoryFullReportItem>();
    }

    public class InventoryCategoryReportItem
    {
        public string Category { get; set; } = string.Empty;
        public int ItemTypes { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class LowStockReportItem
    {
        public int ItemId { get; set; }
        public string UPC { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Points { get; set; }
    }

    public class InventoryFullReportItem
    {
        public int ItemId { get; set; }
        public string UPC { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Subcategory { get; set; } = string.Empty;
        public string UnitSize { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Points { get; set; }
    }

    public class StudentReportViewModel
    {
        public int TotalStudentsWithOrders { get; set; }
        public int TotalOrders { get; set; }
        public int TotalPointsUsed { get; set; }
        public int TotalItemsOrdered { get; set; }


        public List<StudentOrderReportItem> StudentOrders { get; set; } = new List<StudentOrderReportItem>();
    }

    public class StudentOrderReportItem
    {
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public int TotalPointsUsed { get; set; }
        public int TotalItemsOrdered { get; set; }
        public DateTime MostRecentOrder { get; set; }
    }

    public class UsageReportViewModel
    {
        public int TotalOrders { get; set; }
        public int OnlineOrders { get; set; }
        public int KioskOrders { get; set; }
        public int FulfilledOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalPointsUsed { get; set; }
        public int TotalItemsOrdered { get; set; }

        public List<PopularItemReportItem> MostRequestedItems { get; set; } = new List<PopularItemReportItem>();
        public List<OrderDateReportItem> OrdersByDate { get; set; } = new List<OrderDateReportItem>();
    }

    public class PopularItemReportItem
    {
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int TotalQuantityOrdered { get; set; }
        public int TotalPointsUsed { get; set; }
    }

    public class OrderDateReportItem
    {
        public DateTime OrderDate { get; set; }
        public int OrderCount { get; set; }
        public int TotalPoints { get; set; }
    }
}