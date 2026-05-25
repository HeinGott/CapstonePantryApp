using Pantreats.Models;
using System.IO;
using Pantreats.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Pantreats.Areas.Identity.Pages.Account
{
    public class ShopModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public List<Inventory> Items { get; set; } = new();

        [BindProperty]
        public string? RequestItem { get; set; }

        [BindProperty]
        public List<string>? SelectedItems { get; set; }
        public ShopModel(ApplicationDbContext context)
        {
            _context = context;
        }
        public void OnGet()
        {
            Items.Clear();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "FakePanTreatInventory.csv");

            var lines = System.IO.File.ReadAllLines(filePath);

            foreach(var line in lines.Skip(1))
            {
                var data = line.Split(',');

                Items.Add(new Inventory
                {
                    UPC = data[0],
                    ItemName = data[1],
                    BrandName = data[2],
                    Category = data[3],
                    Subcategory = data[4],
                    GenderUse = data[5],
                    UnitSize = data[6],
                    Quantity = int.Parse(data[7]),
                });
            }

        }
        public IActionResult OnPost()
        {
            Items.Clear();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "FakePanTreatInventory.csv");

            var lines = System.IO.File.ReadAllLines(filePath);

            foreach (var line in lines.Skip(1))
            {
                var data = line.Split(',');

                Items.Add(new Inventory
                {
                    UPC = data[0],
                    ItemName = data[1],
                    BrandName = data[2],
                    Category = data[3],
                    Subcategory = data[4],
                    GenderUse = data[5],
                    UnitSize = data[6],
                    Quantity = int.Parse(data[7]),
                });
            }

            var request = new ItemRequest
            {
                SelectedItems = SelectedItems != null ? string.Join(",", SelectedItems) : "",
                RequestedItem = RequestItem,
                UserName = User.Identity?.Name ?? "Anonymous"
            };
            _context.ItemRequest.Add(request);
            _context.SaveChanges();

            return RedirectToPage("/Account/Shop");
        }

    }
}