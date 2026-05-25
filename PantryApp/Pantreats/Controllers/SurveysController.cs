using Microsoft.AspNetCore.Mvc;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class SurveysController : Controller
    {
        public IActionResult FacultySurvey()
        {
            return View();
        }
    }
}