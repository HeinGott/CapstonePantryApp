namespace Pantreats.Models
{
    public class RecipeIndexViewModel
    {
        public List<Recipe> Recipes { get; set; } = new();

        public bool IsAdmin { get; set; }

        public string? SelectedDiet { get; set; }

    }
}
