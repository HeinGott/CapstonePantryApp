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
                .Take(20)
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

            return View(order);
        }

        //this will delete an order, but only if you're an admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var order = _context.Orders.FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            _context.SaveChanges();

            return RedirectToAction("History");
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