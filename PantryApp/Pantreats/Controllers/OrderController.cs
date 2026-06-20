using Microsoft.AspNetCore.Mvc;
using Pantreats.Services;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Students")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public OrderController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult History()
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.OrderItems);

            if (!User.IsInRole("Admin"))
            {
                var userName = User.Identity!.Name; 

                query = query.Where(o => o.UserId == userName);
            }

            var orders = query
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var userId = User.Identity!.Name;

                if (order.UserId != userId)
                {
                    return NotFound();
                }
            }

            if (User.IsInRole("Admin"))
            {
                ViewBag.InventoryItems = _context.Inventory
                    .OrderBy(i => i.ItemName)
                    .ToList();
            }

            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteItem(int id)
        {
            var item = _context.OrderItems.FirstOrDefault(i => i.OrderItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            var orderId = item.OrderId;

            //this will restore the stock
            if (item.InventoryUPC != null)
            {
                var inventory = item.InventoryItemId.HasValue
                    ? _context.Inventory.FirstOrDefault(inv => inv.ItemId == item.InventoryItemId.Value)
                    : _context.Inventory.FirstOrDefault(inv => inv.UPC == item.InventoryUPC);
                if (inventory != null)
                {
                    inventory.Quantity += item.OrderQuantity;
                }
            }

            //remove the item from the order
            _context.OrderItems.Remove(item);

            //recalculate the order total
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order != null)
            {
                order.Total = order.OrderItems
                    .Where(i => i.OrderItemId != id)   // skip the one we just removed (still tracked until save)
                    .Sum(i => i.Points * i.OrderQuantity);
            }

            _context.SaveChanges();

            return RedirectToAction("Details", new { id = orderId });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult AddItem(int id, string upc, int quantity)
        {
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // re-look-up the inventory item to ensure we have the latest stock and points values (don't trust the form)
            var inventory = _context.Inventory.FirstOrDefault(inv => inv.UPC == upc);
            if (inventory == null)
            {
                return NotFound();
            }

            // quantity must be positive AND not exceed available stock (so stock can't go negative)
            if (quantity < 1 || quantity > inventory.Quantity)
            {
                TempData["AddItemError"] = $"Invalid quantity for {inventory.ItemName}.";
                return RedirectToAction("Details", new { id });
            }

            // building a mini inventory to add items to the order
            var item = new OrderItem
            {
                InventoryItemId = inventory.ItemId,
                InventoryUPC = inventory.UPC,
                ItemName = inventory.ItemName,
                Category = inventory.Category,
                OrderQuantity = quantity,
                Points = inventory.Points
            };

            order.OrderItems.Add(item); // add the item to the order
            inventory.Quantity -= quantity; //updating the quanity in stock
            order.Total = order.OrderItems.Sum(i => i.Points * i.OrderQuantity); // recompute the total based on all items in the order

            _context.SaveChanges();

            return RedirectToAction("Details", new { id });
        }


        [AllowAnonymous]
        public async Task<IActionResult> TestEmail()
        {
            var sent = await _emailService.SendOrderConfirmationAsync(
                "jakegmain@gmail.com", 999, "https://localhost/Order/Details/999"); //we use this for the checkout page, user email, order id, the order detail url

            return Content(sent ? "Sent — check your email." : "Send failed — check the Output window.");
        }
    }

}
