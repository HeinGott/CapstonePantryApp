using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Volunteers")]
    public class OrderFulfilmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderFulfilmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchText, string? statusFilter)
        {
            var allFulfilments = await _context.OrderFulfilments
                .AsNoTracking()
                .Include(fulfilment => fulfilment.Order)
                    .ThenInclude(order => order.OrderItems)
                .Where(fulfilment => fulfilment.Order == null ||
                                     string.IsNullOrWhiteSpace(fulfilment.Order.OrderSource) ||
                                     fulfilment.Order.OrderSource != Order.SourceKiosk)
                .ToListAsync();

            var fulfilments = allFulfilments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                fulfilments = fulfilments.Where(fulfilment =>
                    fulfilment.OrderId.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(fulfilment.Order?.Email) &&
                     fulfilment.Order.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                fulfilments = fulfilments.Where(fulfilment =>
                    string.Equals(OrderFulfilment.NormalizeStatus(fulfilment.OrderStatus), statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            var filteredFulfilments = fulfilments
                .OrderBy(fulfilment => GetStatusRank(fulfilment.OrderStatus))
                .ThenByDescending(fulfilment => fulfilment.Order?.OrderDate ?? DateTime.MinValue)
                .ToList();

            var model = new OrderFulfilmentViewModel
            {
                OrderFulfilments = filteredFulfilments,
                TotalOrders = allFulfilments.Count,
                OrderPlacedCount = allFulfilments.Count(fulfilment =>
                    string.Equals(OrderFulfilment.NormalizeStatus(fulfilment.OrderStatus), OrderFulfilment.StatusOrderPlaced, StringComparison.OrdinalIgnoreCase)),
                ReadyForPickupCount = allFulfilments.Count(fulfilment =>
                    string.Equals(OrderFulfilment.NormalizeStatus(fulfilment.OrderStatus), OrderFulfilment.StatusReadyForPickup, StringComparison.OrdinalIgnoreCase)),
                CompletedCount = allFulfilments.Count(fulfilment =>
                    string.Equals(OrderFulfilment.NormalizeStatus(fulfilment.OrderStatus), OrderFulfilment.StatusCompleted, StringComparison.OrdinalIgnoreCase)),
                SearchText = searchText,
                StatusFilter = statusFilter
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var fulfilment = await _context.OrderFulfilments
                .AsNoTracking()
                .FirstOrDefaultAsync(fulfilment => fulfilment.Id == id);

            if (fulfilment == null)
            {
                return NotFound();
            }

            return RedirectToAction("Details", "Order", new { id = fulfilment.OrderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrderFulfilment fulfilment)
        {
            return RedirectToAction("Details", "Order", new { id = fulfilment.OrderId });
        }

        private static int GetStatusRank(string? status)
        {
            return OrderFulfilment.NormalizeStatus(status) switch
            {
                OrderFulfilment.StatusOrderPlaced => 0,
                OrderFulfilment.StatusReadyForPickup => 1,
                OrderFulfilment.StatusCompleted => 2,
                _ => 3
            };
        }
    }
}
