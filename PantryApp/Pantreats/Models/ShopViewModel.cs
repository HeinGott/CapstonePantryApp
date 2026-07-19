namespace Pantreats.Models
{
    public class ShopViewModel
    {
        public List<ShopItemViewModel> Items { get; set; } = new();
        public List<string> Subcategories { get; set; } = new();
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int TotalInventoryItems { get; set; }
        public int PageSize { get; set; }
        public string? SearchTerm { get; set; }
        public string? SelectedSubcategory { get; set; }
        public int? MonthlyPointBalance { get; set; }
        public int? CurrentPointBalance { get; set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(SearchTerm) || !string.IsNullOrWhiteSpace(SelectedSubcategory);
    }

    public class ShopItemViewModel
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
        public string ImageName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class ShopAddCartRequest
    {
        public string UPC { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; } = 1;
    }

    public class ShopCheckoutRequest
    {
        public List<string> UPCs { get; set; } = new();
        public string? RequestItem { get; set; }
    }
}
