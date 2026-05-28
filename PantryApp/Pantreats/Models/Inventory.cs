using System.ComponentModel.DataAnnotations;



namespace Pantreats.Models
{
    public class Inventory
    {
        [Key]
        public string UPC { get; set; }
        public string ItemName { get; set; }
        public string BrandName { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string GenderUse { get; set; }
        public string UnitSize { get; set; }
        public int Quantity { get; set; }
    }
}


