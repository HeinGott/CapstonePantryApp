using Microsoft.AspNetCore.Mvc;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class SurveyController : Controller
    {
        public IActionResult Survey()
        {
            return View();
        }
    }
}