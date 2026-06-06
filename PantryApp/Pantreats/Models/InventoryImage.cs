using System.ComponentModel.DataAnnotations.Schema;

namespace Pantreats.Models
{
    public class InventoryImage
    {
        [Key]
        public string UPC {  get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[] ImageData { get; set; }
        public string ContentType { get; set; } //content type meaning file type like jpg/png

        public Inventory Inventory { get; set; } //navigation property
    }
}
