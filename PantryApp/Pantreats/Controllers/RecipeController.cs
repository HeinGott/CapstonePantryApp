using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Pantreats.Controllers
{
    public class RecipeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RecipeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? diet, string? mealType, string? search)
        {
            var recipesQuery = _context.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.InventoryItem)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(diet))
            {
                recipesQuery = recipesQuery.Where(r =>
                    r.DietaryTags != null &&
                    r.DietaryTags.Contains(diet));
            }

            if (!string.IsNullOrWhiteSpace(mealType))
            {
                recipesQuery = recipesQuery.Where(r =>
                    r.MealType == mealType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                recipesQuery = recipesQuery.Where(r =>
                    r.Title.Contains(search) ||
                    (r.Description != null && r.Description.Contains(search)) ||
                    (r.Instructions != null && r.Instructions.Contains(search)) ||
                    r.Ingredients.Any(i =>
                        (i.CustomIngredientName != null && i.CustomIngredientName.Contains(search)) ||
                        (i.SubstituteIngredientName != null && i.SubstituteIngredientName.Contains(search))));
            }

            var model = new RecipeIndexViewModel
            {
                Recipes = await recipesQuery.ToListAsync(),
                IsAdmin = User.IsInRole("Admin"),
                SelectedDiet = diet
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.InventoryItem)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound();

            return View(recipe);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile? recipeFile)
        {
            if (recipeFile == null || recipeFile.Length == 0)
            {
                ModelState.AddModelError("", "Please choose a recipe file.");
                return View();
            }

            string recipeText;

            var extension = Path.GetExtension(recipeFile.FileName).ToLower();

            if (extension == ".txt")
            {
                using var reader = new StreamReader(recipeFile.OpenReadStream());
                recipeText = await reader.ReadToEndAsync();
            }
            else if (extension == ".docx")
            {
                using var stream = recipeFile.OpenReadStream();
                using var document = WordprocessingDocument.Open(stream, false);

                recipeText = document.MainDocumentPart!
                    .Document
                    .Body!
                    .InnerText;
            }
            else
            {
                ModelState.AddModelError("", "Only .txt and .docx recipe files are supported.");
                return View();
            }

            var recipe = new Recipe
            {
                Title = Path.GetFileNameWithoutExtension(recipeFile.FileName),
                Description = "",
                Instructions = ""
            };

            var ingredients = new List<RecipeIngredient>();

            string section = "";

            foreach (var rawLine in recipeText.Split('\n'))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Equals("Title:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "Title";
                    continue;
                }

                if (line.Equals("Description:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "Description";
                    continue;
                }

                if (line.Equals("Meal Type:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "MealType";
                    continue;
                }

                if (line.Equals("Ingredients:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "Ingredients";
                    continue;
                }

                if (line.Equals("Instructions:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "Instructions";
                    continue;
                }

                switch (section)
                {
                    case "Title":
                        recipe.Title = line;
                        break;

                    case "Description":
                        recipe.Description += line + Environment.NewLine;
                        break;

                    case "MealType":
                        recipe.MealType = line;
                        break;

                    case "Ingredients":
                        var knownUnits = new[]
                        {
                            "cup", "cups",
                            "tbsp", "tsp",
                            "oz", "lb",
                            "can", "cans",
                            "package", "packages",
                            "slice", "slices",
                            "pinch"
                        };

                        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        var quantity = "";
                        var unit = "";
                        var ingredientName = line;

                        if (words.Length >= 2 && knownUnits.Contains(words[1], StringComparer.OrdinalIgnoreCase))
                        {
                            quantity = words[0];
                            unit = words[1];
                            ingredientName = string.Join(" ", words.Skip(2));
                        }

                        ingredients.Add(new RecipeIngredient
                        {
                            Quantity = quantity,
                            Unit = unit,
                            ImportedIngredientName = ingredientName
                        });

                        break;

                    case "Instructions":
                        recipe.Instructions += line + Environment.NewLine;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(recipe.Instructions))
            {
                recipe.Instructions = recipeText;
            }

            var inventoryItems = await _context.Inventory
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            var model = new RecipeEditorViewModel
            {
                Recipe = recipe,
                Ingredients = ingredients,
                InventoryItems = inventoryItems,
                RecipeIngredientNames = inventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                    .Select(i => i.ItemName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList(),

                    IsImportedRecipe = true
            };

            ModelState.Clear();

            return View("Create", model);
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var inventoryItems = await _context.Inventory
                .OrderBy(i => i.Category)
                .ThenBy(i => i.ItemName)
                .ToListAsync();

            var model = new RecipeEditorViewModel
            {
                InventoryItems = inventoryItems,

                RecipeIngredientNames = inventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                    .Select(i => i.ItemName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList(),

                IsImportedRecipe = false
            };


            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeEditorViewModel model)
        {
            ModelState.Remove("Recipe.Ingredients");
            ModelState.Remove("Recipe.DietaryTags");
            ModelState.Remove("Recipe.ImagePath");

            for (int i = 0; i < model.Ingredients.Count; i++)
            {
                ModelState.Remove($"Ingredients[{i}].Recipe");
                ModelState.Remove($"Ingredients[{i}].InventoryItem");
            }

            if (!ModelState.IsValid)
            {
                model.InventoryItems = await _context.Inventory
                    .OrderBy(i => i.Category)
                    .ThenBy(i => i.ItemName)
                    .ToListAsync();

                model.RecipeIngredientNames = model.InventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                    .Select(i => i.ItemName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                return View(model);
            }

            var recipe = model.Recipe;

            recipe.DietaryTags = BuildDietaryTags(recipe);

            if (model.RecipeImage != null && model.RecipeImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "recipes"
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.RecipeImage.FileName);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.RecipeImage.CopyToAsync(stream);
                }

                recipe.ImagePath = "/images/recipes/" + fileName;
            }

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            foreach (var ingredient in model.Ingredients)
            {
                if (!string.IsNullOrWhiteSpace(ingredient.CustomIngredientName) ||
                    !string.IsNullOrWhiteSpace(ingredient.Quantity) ||
                    !string.IsNullOrWhiteSpace(ingredient.Unit))
                {
                    var newIngredient = new RecipeIngredient
                    {
                        RecipeId = recipe.RecipeId,
                        CustomIngredientName = string.IsNullOrWhiteSpace(ingredient.CustomIngredientName)
    ? ingredient.ImportedIngredientName
    : ingredient.CustomIngredientName,

                        ImportedIngredientName = ingredient.ImportedIngredientName,
                        SubstituteIngredientName = ingredient.SubstituteIngredientName,
                        Quantity = ingredient.Quantity,
                        Unit = ingredient.Unit,
                        InventoryItemId = ingredient.InventoryItemId
                    };

                    _context.RecipeIngredients.Add(newIngredient);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = recipe.RecipeId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Preview(RecipeEditorViewModel model)
        {
            model.Recipe.DietaryTags = BuildDietaryTags(model.Recipe);

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIngredient(int id)
        {
            var ingredient = await _context.RecipeIngredients.FindAsync(id);

            if (ingredient == null)
                return NotFound();

            var recipeId = ingredient.RecipeId;

            _context.RecipeIngredients.Remove(ingredient);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = recipeId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditIngredient(int id)
        {
            var ingredient = await _context.RecipeIngredients.FindAsync(id);

            if (ingredient == null)
                return NotFound();

            ViewBag.InventoryItems = await _context.Inventory
                .OrderBy(item => item.ItemName)
                .ToListAsync();

            return View(ingredient);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIngredient(
        int RecipeIngredientId,
        int RecipeId,
        int? InventoryItemId,
        string? CustomIngredientName,
        string? Quantity,
        string? Unit)
        {
            var existingIngredient = await _context.RecipeIngredients
                .FirstOrDefaultAsync(i => i.RecipeIngredientId == RecipeIngredientId);

            if (existingIngredient == null)
                return NotFound();

            existingIngredient.InventoryItemId = InventoryItemId;
            existingIngredient.CustomIngredientName = CustomIngredientName;
            existingIngredient.Quantity = Quantity;
            existingIngredient.Unit = Unit;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = RecipeId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddIngredient(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);

            if (recipe == null)
                return NotFound();

            ViewBag.Recipe = recipe;
            ViewBag.InventoryItems = await _context.Inventory
                .OrderBy(item => item.ItemName)
                .ToListAsync();

            return View(new RecipeIngredient
            {
                RecipeId = id
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient(RecipeIngredient ingredient)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Recipe = await _context.Recipes.FindAsync(ingredient.RecipeId);
                ViewBag.InventoryItems = await _context.Inventory
                    .OrderBy(item => item.ItemName)
                    .ToListAsync();

                return View(ingredient);
            }

            _context.RecipeIngredients.Add(ingredient);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = ingredient.RecipeId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound();

            var model = new RecipeEditorViewModel
            {
                Recipe = recipe
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RecipeEditorViewModel model)
        {
            ModelState.Remove("Recipe.Ingredients");
            ModelState.Remove("Recipe.DietaryTags");
            ModelState.Remove("Recipe.ImagePath");

            for (int i = 0; i < model.Ingredients.Count; i++)
            {
                ModelState.Remove($"Ingredients[{i}].Recipe");
                ModelState.Remove($"Ingredients[{i}].InventoryItem");
            }

            if (!ModelState.IsValid)
            {
                model.InventoryItems = await _context.Inventory
                    .OrderBy(i => i.Category)
                    .ThenBy(i => i.ItemName)
                    .ToListAsync();

                model.RecipeIngredientNames = model.InventoryItems
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemName))
                    .Select(i => i.ItemName)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();

                return View(model);
            }

            var existingRecipe = await _context.Recipes
                .FirstOrDefaultAsync(r => r.RecipeId == model.Recipe.RecipeId);

            if (existingRecipe == null)
                return NotFound();

            existingRecipe.Title = model.Recipe.Title;
            existingRecipe.Description = model.Recipe.Description;
            existingRecipe.MealType = model.Recipe.MealType;
            existingRecipe.Instructions = model.Recipe.Instructions;

            existingRecipe.IsDairyFree = model.Recipe.IsDairyFree;
            existingRecipe.IsGlutenFree = model.Recipe.IsGlutenFree;
            existingRecipe.IsVegetarian = model.Recipe.IsVegetarian;
            existingRecipe.IsVegan = model.Recipe.IsVegan;
            existingRecipe.IsNutFree = model.Recipe.IsNutFree;

            existingRecipe.DietaryTags = BuildDietaryTags(existingRecipe);

            if (model.RecipeImage != null && model.RecipeImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "images",
                    "recipes"
                );

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.RecipeImage.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.RecipeImage.CopyToAsync(stream);
                }

                existingRecipe.ImagePath = "/images/recipes/" + fileName;
            }

            foreach (var ingredient in model.Ingredients)
            {
                if (ingredient.RecipeIngredientId > 0)
                {
                    var existingIngredient = await _context.RecipeIngredients
                        .FirstOrDefaultAsync(i => i.RecipeIngredientId == ingredient.RecipeIngredientId);

                    if (existingIngredient != null)
                    {
                        existingIngredient.Quantity = ingredient.Quantity;
                        existingIngredient.Unit = ingredient.Unit;
                        existingIngredient.CustomIngredientName = ingredient.CustomIngredientName;
                        existingIngredient.SubstituteIngredientName = ingredient.SubstituteIngredientName;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(ingredient.CustomIngredientName) ||
                         !string.IsNullOrWhiteSpace(ingredient.Quantity) ||
                         !string.IsNullOrWhiteSpace(ingredient.Unit))
                {
                    var newIngredient = new RecipeIngredient
                    {
                        RecipeId = existingRecipe.RecipeId,
                        Quantity = ingredient.Quantity,
                        Unit = ingredient.Unit,
                        CustomIngredientName = string.IsNullOrWhiteSpace(ingredient.CustomIngredientName)
    ? ingredient.ImportedIngredientName
    : ingredient.CustomIngredientName,

                        ImportedIngredientName = ingredient.ImportedIngredientName,
                        SubstituteIngredientName = ingredient.SubstituteIngredientName,
                    };

                    _context.RecipeIngredients.Add(newIngredient);
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = existingRecipe.RecipeId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.RecipeId == id);

            if (recipe == null)
                return NotFound();

            _context.RecipeIngredients.RemoveRange(recipe.Ingredients);
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static string CleanIngredientName(string itemName)
        {
            var name = itemName;

            var removeWords = new[]
            {
                "Great Value", "Food Lion", "Publix", "Laura Lynn", "Lidl",
                "Happy Harvest", "Campbell", "Campbell's", "Del Monte",
                "Dakota's Pride", "Northern Catch", "Chef's Cupboard",
                "Member's Mark", "Kirkland", "Barilla", "Combino",
                "Mueller's", "American Beauty"
            };

            foreach (var word in removeWords)
            {
                name = name.Replace(word, "", StringComparison.OrdinalIgnoreCase);
            }

            name = name.Replace("No Salt Added", "", StringComparison.OrdinalIgnoreCase);
            name = name.Replace("No Salt", "", StringComparison.OrdinalIgnoreCase);

            return name.Trim();
        }

        private static string BuildDietaryTags(Recipe recipe)
        {
            return string.Join(", ", new[]
            {
                recipe.IsDairyFree ? "Dairy-Free" : "Contains Dairy",
                recipe.IsGlutenFree ? "Gluten-Free" : "Contains Gluten",
                recipe.IsVegetarian ? "Vegetarian" : "Contains Meat",
                recipe.IsVegan ? "Vegan" : "Contains Animal Products",
                recipe.IsNutFree ? "Nut-Free" : "Contains Nuts"
            });
        }

    }

}