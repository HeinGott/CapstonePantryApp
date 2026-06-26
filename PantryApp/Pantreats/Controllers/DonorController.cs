using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;
using Pantreats.Models;
using Microsoft.AspNetCore.Authorization;


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

        // shows all donors
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var donor = await _context.Donors.ToListAsync();
            return View(donor);
        }

        // show add donor page
        public IActionResult Create()
        {
            return View();
        }

        // save new donor
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

        // show one donor
        public async Task<IActionResult> Details(int id)
        {
            var donor = await _context.Donors
                .FirstOrDefaultAsync(d => d.DonorID == id);

            if (donor == null)
            {
                return NotFound();
            }

            donor.Donations = await _context.Donations
                .Include(d => d.DonationItems)
                .Where(d => d.DonorId == donor.DonorID)
                .OrderByDescending(d => d.DonationDate)
                .ToListAsync();

            return View(donor);
        }

        // show edit page
        public async Task<IActionResult> Edit(int id)
        {
            var donor = await _context.Donors.FindAsync(id);
            if (donor == null) return NotFound();
            return View(donor);
        }

        // save edits
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Donor donor)
        {
            if (id != donor.DonorID) return NotFound();

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
            if (donor == null) return NotFound();

            _context.Donors.Remove(donor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // register page
        public IActionResult Register()
        {
            return View();
        }

        // save new registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Donor model, string Password)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Donors");

                    var donor = new Donor
                    {
                        Name = model.Name,
                        Email = model.Email,
                        PhoneNumber = model.PhoneNumber,
                        UserId = user.Id
                    };

                    _context.Donors.Add(donor);
                    await _context.SaveChangesAsync();

                    return RedirectToPage("/Account/RegisterConfirmation", new { area = "Identity", email = model.Email });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // login page
        public IActionResult Login()
        {
            return View();
        }

        // handle login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, bool RememberMe)
        {
            var result = await _signInManager.PasswordSignInAsync(
                Email, Password, RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Dashboard", "Donor");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View();
        }

        // donor dashboard
        [Authorize(Roles = "Donors")]
        public IActionResult Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                return View(new DonorDashboardViewModel
                {
                    DonorName = User.Identity?.Name ?? "Donor",
                    TotalDonations = 0,
                    TotalItemsDonated = 0,
                    LastDonationDate = null,
                    Donations = new List<Donation>()
                });
            }

            var donations = _context.Donations
                .Include(d => d.DonationItems)
                .Where(d => d.DonorId == donor.DonorID)
                .OrderByDescending(d => d.DonationDate)
                .ToList();

            return View(new DonorDashboardViewModel
            {
                DonorName = donor.Name,
                TotalDonations = donations.Count,
                TotalItemsDonated = donations.SelectMany(d => d.DonationItems).Sum(i => i.Quantity),
                LastDonationDate = donations.FirstOrDefault()?.DonationDate,
                Donations = donations
            });
        }

        // show create donation page
        public IActionResult CreateDonation()
        {
            return View();
        }

        // Creates a new donation request from the donor and stores the selected items.
        // save donation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDonation(List<DonationItemInput> items, string? comment, string? address)
        {
            var userId = _userManager.GetUserId(User);
            var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

            if (donor == null) return RedirectToAction("Dashboard");

            var selected = items.Where(i => i.Selected).ToList();
            if (!selected.Any()) return RedirectToAction("CreateDonation");

            var donation = new Donation
            {
                DonorId = donor.DonorID,
                DonationDate = DateTime.Now,
                Status = "Pending",
                Address = address,
                Comment = comment,
                DonationItems = new List<DonationItem>()
            };

            foreach (var item in selected)
            {
                donation.DonationItems.Add(new DonationItem
                {
                    ItemName = item.Name,
                    Quantity = item.Quantity
                });
            }

            _context.Donations.Add(donation);
            _context.SaveChanges();

            return RedirectToAction("DonationSubmitted");
        }

        // donation submitted confirmation
        public IActionResult DonationSubmitted()
        {
            return View();
        }
    }
}