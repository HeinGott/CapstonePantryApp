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
        public IActionResult OnGet()
        {
            return RedirectToAction("Index", "Shop", new { area = "" });
        }

        public IActionResult OnPost()
        {
            return RedirectToAction("Index", "Shop", new { area = "" });
        }
    }
}
