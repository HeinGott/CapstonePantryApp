using System.ComponentModel.DataAnnotations;
namespace Pantreats.Models
{
    public class RecipeIngredient
    {
        public int RecipeIngredientId { get; set; }

        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;

        public int? InventoryItemId { get; set; }
        public Inventory? InventoryItem { get; set; }

        public string? CustomIngredientName { get; set; }
        public string? ImportedIngredientName { get; set; }
        public string? SubstituteIngredientName { get; set; }

        [RegularExpression(@"^[0-9\s\/.]+$", ErrorMessage = "Quantity can only contain numbers, spaces, decimals, or fractions.")]
        public string? Quantity { get; set; }

        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Unit can only contain letters.")]
        public string? Unit { get; set; }
    }
}
