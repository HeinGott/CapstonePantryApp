using Microsoft.AspNetCore.Mvc;

namespace Pantreats.Controllers
{
    public class InventoryController : Controller
    {
        public IActionResult Index()
        {
            // Create a list to store inventory items
            var inventory = new List<Inventory>();

            // Get path to the CSV file
            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "App_Data",
                "FakePanTreatInventory.csv"
            );

            // Check if file exists
            if (System.IO.File.Exists(filePath))
            {
                // Read all lines and skip the header (first row)
                var lines = System.IO.File.ReadAllLines(filePath).Skip(1);

                // Loop through each line in the file
                foreach (var line in lines)
                {
                    // Split line by commas
                    var parts = line.Split(',');

                    // Add item to list
                    inventory.Add(new Inventory
                    {
                        UPC = parts[0],
                        ItemName = parts[1],
                        BrandName = parts[2],
                        Category = parts[3],
                        Subcategory = parts[4],     // Column D
                        GenderUse = parts[5],    // Column E
                        UnitSize = parts[6],     // Column F
                        Quantity = int.Parse(parts[7].Trim()), // Column G
                    });
                }
            }

            // Store total count in ViewBag
            ViewBag.Count = inventory.Count;

            // Send list to the view
            return View(inventory);
        }
    }
}
