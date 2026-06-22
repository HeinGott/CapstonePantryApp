using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class OrderFulfilmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderFulfilmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchText, string? statusFilter)
        {
            var fulfilment = await _context.OrderFulfilments
            .Include(ful => ful.Order)
            .ToListAsync();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                fulfilment = fulfilment
                    .Where(ful =>
                        ful.OrderId.ToString().Contains(searchText) ||
                        (ful.Order != null &&
                         ful.Order.Email.Contains(searchText)))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                fulfilment = fulfilment
                    .Where(ful => ful.OrderStatus == statusFilter)
                    .ToList();
            }

            var model = new OrderFulfilmentViewModel
            {
                OrderFulfilments = fulfilment,
                TotalOrders = fulfilment.Count,
                WaitingPickupOrders = fulfilment.Count(ful => ful.OrderStatus == "Waiting Pickup"),
                FulfilledOrders = fulfilment.Count(ful => ful.OrderStatus == "Fulfilled"),
                SearchText = searchText,
                StatusFilter = statusFilter
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var fulfilment = await _context.OrderFulfilments
                .FirstOrDefaultAsync(ful => ful.Id == id);

            if (fulfilment == null)
            {
                return NotFound();
            }

            return View(fulfilment);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(OrderFulfilment fulfilment)
        {
            if (ModelState.IsValid)
            {
                _context.OrderFulfilments.Update(fulfilment);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(fulfilment);
        }

    }
}
