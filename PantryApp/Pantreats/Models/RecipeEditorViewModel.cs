namespace Pantreats.Models
{
    public class RecipeEditorViewModel
    {
        public Recipe Recipe { get; set; } = new Recipe();

        public List<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();

        public IFormFile? RecipeImage { get; set; }

        public IFormFile? RecipeDocument { get; set; }

        public List<Inventory> InventoryItems { get; set; } = new List<Inventory>();

        public List<string> RecipeIngredientNames { get; set; } = new List<string>();

        public bool IsImportedRecipe { get; set; }
    }
}
