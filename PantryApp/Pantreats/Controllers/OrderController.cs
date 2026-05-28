using Microsoft.AspNetCore.Mvc;

namespace Pantreats.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult History()
        {
            return View();
        }
    }
}
