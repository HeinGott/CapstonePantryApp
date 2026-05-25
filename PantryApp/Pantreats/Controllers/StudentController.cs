using Microsoft.AspNetCore.Mvc;
using Pantreats.Models;

namespace Pantreats.Controllers
{    
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            ViewBag.StudentId = id;
            return View();
        }

        public IActionResult Apply()
        {
            var model = new UserApplicationViewModel();
            return View(model);
        }

        [HttpPost]
        public IActionResult UserApplicationForm(UserApplicationViewModel model)
        {
            if (model.DOB > DateTime.Today)
            {
                ModelState.AddModelError("DOB", "Date of birth cannot be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View("Apply", model);
            }

            var application = new UserApplication
            {
                UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value,

                StudentId = model.StudentId,
                FirstName = model.FirstName,
                MiddleName = model.MiddleName,
                LastName = model.LastName,
                DOB = model.DOB,
                PhoneNum = model.PhoneNum,
                Gender = model.Gender,
                StudentStatus = model.StudentStatus,

                HouseholdBabiesToddlers = model.HouseholdBabiesToddlers,
                HouseholdBabiesChildren = model.HouseholdBabiesChildren,
                HouseholdTeens = model.HouseholdTeens,
                HouseholdAdults = model.HouseholdAdults,

                HasTransportation = model.HasTransportation,
                EmploymentStatus = model.EmploymentStatus,
                EmployedHouseMembers = model.EmployedHouseMembers,

                HasSNAP = model.HasSNAP,
                HasWIC = model.HasWIC,
                HasTANF = model.HasTANF,

                IsInterestedInSNAP = model.IsInterestedInSNAP,
                IsInterestedInWIC = model.IsInterestedInWIC,
                IsInterestedInTANF = model.IsInterestedInTANF,

                Campus = model.Campus,
                IsActive = true
            };

            _context.UserApplications.Add(application);
            _context.SaveChanges();

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }

    }
}
