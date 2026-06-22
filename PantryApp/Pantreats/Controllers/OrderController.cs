using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;
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
            var accessResult = EnsureApprovedStudentAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            IQueryable<Order> query = _context.Orders
                .Include(o => o.OrderItems);

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity!.Name;

                query = query.Where(o => o.UserId == userId || o.UserId == email || o.Email == email);
            }

            var orders = query
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var accessResult = EnsureApprovedStudentAccess();
            if (accessResult != null)
            {
                return accessResult;
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity!.Name;

                if (order.UserId != userId && order.UserId != email && order.Email != email)
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

            _context.OrderItems.Remove(item);

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order != null)
            {
                order.Total = order.OrderItems
                    .Where(i => i.OrderItemId != id)
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

            var inventory = _context.Inventory.FirstOrDefault(inv => inv.UPC == upc);
            if (inventory == null)
            {
                return NotFound();
            }

            if (quantity < 1 || quantity > inventory.Quantity)
            {
                TempData["AddItemError"] = $"Invalid quantity for {inventory.ItemName}.";
                return RedirectToAction("Details", new { id });
            }

            var item = new OrderItem
            {
                InventoryItemId = inventory.ItemId,
                InventoryUPC = inventory.UPC,
                ItemName = inventory.ItemName,
                Category = inventory.Category,
                OrderQuantity = quantity,
                Points = inventory.Points
            };

            order.OrderItems.Add(item);
            inventory.Quantity -= quantity;
            order.Total = order.OrderItems.Sum(i => i.Points * i.OrderQuantity);

            _context.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        private IActionResult? EnsureApprovedStudentAccess()
        {
            if (User.IsInRole("Admin"))
            {
                return null;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Challenge();
            }

            var status = _context.UserApplications
                .AsNoTracking()
                .Where(application => application.UserId == userId)
                .OrderByDescending(application => application.RegistrationDate)
                .ThenByDescending(application => application.ApplicationId)
                .Select(application => application.ApplicationStatus)
                .FirstOrDefault();

            if (status == ApplicationStatuses.Approved)
            {
                return null;
            }

            TempData["ApplicationAccessMessage"] = "Your student application still needs approval before order access is unlocked.";
            return RedirectToAction("Status", "Student");
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestEmail()
        {
            var sent = await _emailService.SendOrderConfirmationAsync(
                "jakegmain@gmail.com", 999, "https://localhost/Order/Details/999");

            return Content(sent ? "Sent — check your email." : "Send failed — check the Output window.");
        }
    }
}
