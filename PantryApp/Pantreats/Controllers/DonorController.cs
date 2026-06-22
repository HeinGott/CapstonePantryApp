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
            var Donor = await _context.Donors.ToListAsync();

            return View(Donor);
        }

        // show one donor
        public async Task<IActionResult> Details(int id)
        {
            var donor = await _context.Donors
                .FirstOrDefaultAsync(v => v.DonorID == id);

            if (donor == null)
            {
                return NotFound();
            }

            return View(donor);
        }

        // show add donor page
        public IActionResult Create()
        {
            return View();
        }

        // save new donor to database
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

                    //return RedirectToAction("Login", "Donor");
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

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(
                Email,
                Password,
                RememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return RedirectToAction("Dashboard", "Donor");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        //created a dashboard for donors to see their donation history and stats
        public IActionResult Dashboard()
        {
            var userId = _userManager.GetUserId(User);

            var donor = _context.Donors
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                return RedirectToAction("Login");
            }

            var donations = _context.Donations
                .Include(d => d.DonationItems)
                .Where(d => d.DonorId == donor.DonorID)
                .OrderByDescending(d => d.DonationDate)
                .ToList();

            var model = new DonorDashboardViewModel
            {
                DonorName = donor.Name,
                TotalDonations = donations.Count,
                TotalItemsDonated = donations
                    .SelectMany(d => d.DonationItems)
                    .Sum(i => i.Quantity),
                LastDonationDate = donations
                    .FirstOrDefault()?.DonationDate,
                Donations = donations
            };

            return View(model);
        }

        public IActionResult CreateDonation()
        {
            return View();
        }

        //THESE METHODS ARE ONLY FOR TESTING PURPOSES 
        public IActionResult DonationSubmitted()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDonation(string[] selectedItems, Dictionary<string, int> quantities)
        {
            var userId = _userManager.GetUserId(User);

            var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                return RedirectToAction("Dashboard");
            }

            var donation = new Donation
            {
                DonorId = donor.DonorID,
                DonationDate = DateTime.Now,
                Status = "Pending",
                DonationItems = new List<DonationItem>()
            };

            foreach (var itemName in selectedItems)
            {
                var quantity = quantities.ContainsKey(itemName)
                    ? quantities[itemName]
                    : 1;

                donation.DonationItems.Add(new DonationItem
                {
                    ItemName = itemName,
                    Quantity = quantity
                });
            }

            _context.Donations.Add(donation);
            _context.SaveChanges();

            return RedirectToAction("DonationSubmitted");
        }
    }
}