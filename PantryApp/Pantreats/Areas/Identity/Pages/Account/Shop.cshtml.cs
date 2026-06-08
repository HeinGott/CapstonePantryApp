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
            Items = _context.Inventory.ToList();
        }

        public IActionResult OnPost()
        {
            // Reload inventory from database (optional, mostly for UI consistency)
            Items = _context.Inventory.ToList();

            // If user selected items, create order
            if (SelectedItems != null && SelectedItems.Any())
            {
                // 1. Create Order
                var order = new Order
                {
                    UserId = User.Identity?.Name ?? "Anonymous",
                    Email = User.Identity?.Name ?? "Anonymous",
                    PhoneNum = "Not Provided",
                    OrderDate = DateTime.Now,
                    Total = SelectedItems.Count
                };

                _context.Orders.Add(order);
                _context.SaveChanges(); // generates OrderId

                // 2. Create OrderItems
                foreach (var selectedUPC in SelectedItems)
                {
                    var inventoryItem = _context.Inventory
                        .FirstOrDefault(i => i.UPC == selectedUPC);

                    if (inventoryItem == null)
                        continue; // skip invalid UPCs safely

                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        InventoryUPC = inventoryItem.UPC,
                        ItemName = inventoryItem.ItemName,
                        Category = inventoryItem.Category,
                        OrderQuantity = 1,
                        Points = 1
                    };

                    _context.OrderItems.Add(orderItem);
                }

                _context.SaveChanges(); // save OrderItems FIRST

                // 3. Create Fulfilment
                var fulfilment = new OrderFulfilment
                {
                    OrderId = order.OrderId,
                    FulfilmentDate = DateTime.Now,
                    OrderStatus = "Waiting Pickup"
                };

                _context.OrderFulfilments.Add(fulfilment);
                _context.SaveChanges();
            }

            // 4. Save item request (unchanged)
            if (!string.IsNullOrWhiteSpace(RequestItem))
            {
                var request = new ItemRequest
                {
                    SelectedItems = SelectedItems != null ? string.Join(",", SelectedItems) : "",
                    RequestedItem = RequestItem,
                    UserName = User.Identity?.Name ?? "Anonymous"
                };

                _context.ItemRequest.Add(request);
                _context.SaveChanges();
            }

            return RedirectToPage("/Account/Shop");
        }
    }
}