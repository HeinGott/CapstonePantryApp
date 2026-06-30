using System.ComponentModel.DataAnnotations;

namespace Pantreats.Models
{
    public class Recipe
    {
        public int RecipeId { get; set; }

        [Required(ErrorMessage = "Recipe title is required.")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Instructions are required.")]
        public string? Instructions { get; set; }

        public string? DietaryTags { get; set; }

        public string? ImagePath { get; set; }

        public string? MealType { get; set; }

        public bool IsDairyFree { get; set; }

        public bool IsGlutenFree { get; set; }

        public bool IsVegetarian { get; set; }

        public bool IsVegan { get; set; }

        public bool IsNutFree { get; set; }

        public List<RecipeIngredient> Ingredients { get; set; } = new();
    }
}
