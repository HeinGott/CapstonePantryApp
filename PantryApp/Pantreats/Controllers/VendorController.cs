using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using static Pantreats.Areas.Identity.Pages.Account.RegisterModel;

namespace Pantreats.Controllers
{
    public class VendorController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        public VendorController(ApplicationDbContext context, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signManager;
        }

        // show all vendors
        public async Task<IActionResult> Index()
        {
            var vendors = await _context.Vendors.ToListAsync();

            return View(vendors);
        }

        // show one vendor
        public async Task<IActionResult> Details(int id)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.VendorID == id);

            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // show add vendor page
        public IActionResult Create()
        {
            return View();
        }

        // save new vendor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Vendor vendor)
        {
            if (ModelState.IsValid)
            {
                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(vendor);
        }

        // show edit page
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        // save edits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Vendor vendor)
        {
            if (id != vendor.VendorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _context.Update(vendor);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = vendor.VendorID });
            }

            return View(vendor);
        }

        // delete vendor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);

            if (vendor == null)
            {
                return NotFound();
            }

            _context.Vendors.Remove(vendor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Vendor model, string Password)
        {
            //Registers user for vendor
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, Password);

                if (result.Succeeded)
                {
                    //Assign vendor role here
                    await _userManager.AddToRoleAsync(user, "Vendors");
                    // Save Vendor to vendor table
                    var vendor = new Vendor
                    {
                        Name = model.Name,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Notes = model.Notes,
                        UserId = user.Id 
                    };
                    //Adds user to vendor Table
                    _context.Vendors.Add(vendor);
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

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }
    }
}