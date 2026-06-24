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
                .Include(order => order.OrderItems)
                .Include(order => order.OrderFulfilment);

            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity!.Name;

                query = query.Where(order => order.UserId == userId || order.UserId == email || order.Email == email);
            }

            var orders = query
                .OrderByDescending(order => order.OrderDate)
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
                .Include(currentOrder => currentOrder.OrderItems)
                .Include(currentOrder => currentOrder.OrderFulfilment)
                .FirstOrDefault(currentOrder => currentOrder.OrderId == id);

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
                var studentApplication = _context.UserApplications
                    .AsNoTracking()
                    .Where(application => application.UserId == order.UserId)
                    .OrderByDescending(application => application.RegistrationDate)
                    .ThenByDescending(application => application.ApplicationId)
                    .FirstOrDefault();

                ViewBag.StudentName = BuildFullName(
                    studentApplication?.FirstName,
                    studentApplication?.MiddleName,
                    studentApplication?.LastName);
                ViewBag.StudentPhone = !string.IsNullOrWhiteSpace(order.PhoneNum) && !string.Equals(order.PhoneNum, "Not Provided", StringComparison.OrdinalIgnoreCase)
                    ? order.PhoneNum
                    : studentApplication?.PhoneNum;
                ViewBag.InventoryItems = _context.Inventory
                    .OrderBy(inventoryItem => inventoryItem.ItemName)
                    .ToList();
            }

            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult MarkReadyForPickup(int id)
        {
            var order = _context.Orders
                .Include(currentOrder => currentOrder.OrderFulfilment)
                .FirstOrDefault(currentOrder => currentOrder.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var fulfilment = GetOrCreateFulfilment(order);
            fulfilment.OrderStatus = OrderFulfilment.StatusReadyForPickup;
            fulfilment.FulfilmentDate = DateTime.Now;
            fulfilment.DateReceived = null;

            _context.SaveChanges();

            TempData["StatusMessage"] = $"Order #{order.OrderId} is now ready for pickup.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult MarkCompleted(int id)
        {
            var order = _context.Orders
                .Include(currentOrder => currentOrder.OrderFulfilment)
                .FirstOrDefault(currentOrder => currentOrder.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var fulfilment = GetOrCreateFulfilment(order);
            if (string.Equals(OrderFulfilment.NormalizeStatus(fulfilment.OrderStatus), OrderFulfilment.StatusOrderPlaced, StringComparison.OrdinalIgnoreCase))
            {
                fulfilment.FulfilmentDate = DateTime.Now;
            }

            fulfilment.OrderStatus = OrderFulfilment.StatusCompleted;
            fulfilment.DateReceived = DateTime.Now;

            _context.SaveChanges();

            TempData["StatusMessage"] = $"Order #{order.OrderId} has been marked completed.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteItem(int id)
        {
            var item = _context.OrderItems.FirstOrDefault(currentItem => currentItem.OrderItemId == id);
            if (item == null)
            {
                return NotFound();
            }

            var orderId = item.OrderId;

            if (item.InventoryUPC != null)
            {
                var inventory = item.InventoryItemId.HasValue
                    ? _context.Inventory.FirstOrDefault(currentInventory => currentInventory.ItemId == item.InventoryItemId.Value)
                    : _context.Inventory.FirstOrDefault(currentInventory => currentInventory.UPC == item.InventoryUPC);
                if (inventory != null)
                {
                    inventory.Quantity += item.OrderQuantity;
                }
            }

            _context.OrderItems.Remove(item);

            var order = _context.Orders
                .Include(currentOrder => currentOrder.OrderItems)
                .FirstOrDefault(currentOrder => currentOrder.OrderId == orderId);

            if (order != null)
            {
                order.Total = order.OrderItems
                    .Where(currentItem => currentItem.OrderItemId != id)
                    .Sum(currentItem => currentItem.Points * currentItem.OrderQuantity);
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
                .Include(currentOrder => currentOrder.OrderItems)
                .FirstOrDefault(currentOrder => currentOrder.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var inventory = _context.Inventory.FirstOrDefault(currentInventory => currentInventory.UPC == upc);
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
            order.Total = order.OrderItems.Sum(currentItem => currentItem.Points * currentItem.OrderQuantity);

            _context.SaveChanges();

            return RedirectToAction("Details", new { id });
        }

        private OrderFulfilment GetOrCreateFulfilment(Order order)
        {
            if (order.OrderFulfilment != null)
            {
                return order.OrderFulfilment;
            }

            var fulfilment = new OrderFulfilment
            {
                OrderId = order.OrderId,
                FulfilmentDate = order.OrderDate,
                OrderStatus = OrderFulfilment.StatusOrderPlaced
            };

            order.OrderFulfilment = fulfilment;
            _context.OrderFulfilments.Add(fulfilment);
            return fulfilment;
        }

        private static string BuildFullName(string? firstName, string? middleName, string? lastName)
        {
            var parts = new[] { firstName, middleName, lastName }
                .Where(namePart => !string.IsNullOrWhiteSpace(namePart));

            return string.Join(" ", parts);
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

            return Content(sent ? "Sent - check your email." : "Send failed - check the Output window.");
        }
    }
}
