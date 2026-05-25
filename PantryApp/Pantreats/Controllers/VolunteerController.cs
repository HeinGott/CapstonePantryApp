using Microsoft.AspNetCore.Mvc;
using Pantreats.Data;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    public class VolunteerController : Controller
    {
        // Bridge to the database
        private readonly ApplicationDbContext _context;

        // Context for the database is injected into the controller via constructor injection
        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET
        public IActionResult ApplyVolunteer()
        {
            var model = new VolunteerApplicationViewModel();
            return View(model);
        }

        // POST
        [HttpPost]
        public IActionResult VolunteerApplicationForm(VolunteerApplicationViewModel model)
        {
            // Re-show the form if validation fails
            if (!ModelState.IsValid)
            {
                return View("ApplyVolunteer", model);
            }

            // Map ViewModel → Entity
            var application = new VolunteerApplication
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value,

                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNum = model.PhoneNum,
                Email = model.Email,
                Year = model.Year,

                HasVolunteeredBefore = model.HasVolunteeredBefore,
                PreviousCapacity = model.PreviousCapacity,
                ReasonForVolunteering = model.ReasonForVolunteering,

                VolunteerFrequency = model.VolunteerFrequency,
                OtherFrequency = model.OtherFrequency,

                MonMorning = model.MonMorning,
                MonAfternoon = model.MonAfternoon,
                TueMorning = model.TueMorning,
                TueAfternoon = model.TueAfternoon,
                WedMorning = model.WedMorning,
                WedAfternoon = model.WedAfternoon,
                ThuMorning = model.ThuMorning,
                ThuAfternoon = model.ThuAfternoon,
                FriMorning = model.FriMorning,
                FriAfternoon = model.FriAfternoon,
                SatMorning = model.SatMorning,
                SatAfternoon = model.SatAfternoon,
                SunMorning = model.SunMorning,
                SunAfternoon = model.SunAfternoon
            };

            // Save to database
            _context.VolunteerApplications.Add(application);
            _context.SaveChanges();

            return RedirectToAction("Success");
        }

        // Success page after submission
        public IActionResult Success()
        {
            return View();
        }
    }
}
