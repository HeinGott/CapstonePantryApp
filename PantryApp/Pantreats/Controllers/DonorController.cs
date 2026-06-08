using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using static Pantreats.Areas.Identity.Pages.Account.RegisterModel;

namespace Pantreats.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        public DonorController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signManager;
        }

        // show all Donors
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Donors.ToListAsync();

            return View(vendors);
        }

        // show one donor
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _context.Donors
                .FirstOrDefaultAsync(v => v.DonorID == id);

            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // show add donor page
        public IActionResult Create()
        {
            return View();
        }

        // save new vendor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Donor donor)
        {
            if (ModelState.IsValid)
            {
                _context.Donors.Add(donor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(donor);
        }

        // show edit page
        public async Task<IActionResult> Edit(int id)
        {
            var donor = await _context.Donors.FindAsync(id);

            if (donor == null)
            {
                return NotFound();
            }

            return View(donor);
        }

        // save edits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Donor donor)
        {
            if (id != donor.DonorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(donor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = donor.DonorID });
            }

            return View(donor);
        }

        // delete donor 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var donor = await _context.Donors.FindAsync(id);

            if (donor == null)
            {
                return NotFound();
            }

            _context.Donors.Remove(donor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Donor model, string Password)
        {
            //Registers user for donor
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, Password);

                if (result.Succeeded)
                {
                    //Assign donor role here
                    await _userManager.AddToRoleAsync(user, "Donors");
                    // Save Donor to donor table
                    var donor = new Donor
                    {
                        Name = model.Name,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Notes = model.Notes,
                        UserId = user.Id 
                    };
                    //Adds user to donor Table
                    _context.Donors.Add(donor);
                    await _context.SaveChangesAsync();

                    //return RedirectToAction("Login", "Vendor");
                    //This redirects you to the default button press to confirm your account
                    //Need to add real emailing feature to register your account
                    return RedirectToPage("/Account/RegisterConfirmation", new { area = "Identity", email = model.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        public IActionResult Register() //add this to show the registration page
        {
            return View();
        }

        public IActionResult Login() //add this to show the login page
        {
            return View();
        }
    }
}