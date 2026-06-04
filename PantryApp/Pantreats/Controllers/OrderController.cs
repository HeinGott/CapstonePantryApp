using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin,Students")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult History()
        {
            // base query — Include pulls the line items along with each order
            IQueryable<Order> query = _context.Orders.Include(o => o.OrderItems);

            // if you're NOT an admin, narrow the query to just your own orders
            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
                query = query.Where(o => o.UserId == userId);
            }
            //only take the 20 most recent orders, and pass them to the view
            var orders = query.OrderByDescending(o => o.OrderDate)
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

            // Students can only view their OWN orders. Admin can view any.
            if (!User.IsInRole("Admin"))
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
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
    }

}