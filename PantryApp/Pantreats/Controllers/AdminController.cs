using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pantreats.Models;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private static readonly Dictionary<string, string> AllowedRoles = new()
        {
            ["Admin"] = "Admin",
            ["Students"] = "Student",
            ["Volunteers"] = "Volunteer",
            ["Donors"] = "Donor"
        };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();

            var model = new List<UserManagerViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleValue = NormalizeRole(roles.FirstOrDefault());

                model.Add(new UserManagerViewModel
                {
                    UserId = user.Id,
                    Email = user.Email ?? string.Empty,
                    Role = GetRoleLabel(roleValue),
                    RoleValue = roleValue
                });
            }

            return View(model);
        }

        public async Task<IActionResult> UserEdit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var model = new UserEditViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = NormalizeRole(roles.FirstOrDefault())
            };

            PopulateRoleOptions(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserEdit(UserEditViewModel model)
        {
            if (!AllowedRoles.ContainsKey(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Select admin, student, volunteer, or donor.");
            }

            if (!ModelState.IsValid)
            {
                PopulateRoleOptions(model);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            user.Email = model.Email;
            user.UserName = model.UserName;
            user.PhoneNumber = model.PhoneNumber;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                PopulateRoleOptions(model);
                return View(model);
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    AddErrors(removeResult);
                    PopulateRoleOptions(model);
                    return View(model);
                }
            }

            await EnsureRoleExists(model.Role);
            var addResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!addResult.Succeeded)
            {
                AddErrors(addResult);
                PopulateRoleOptions(model);
                return View(model);
            }

            TempData["StatusMessage"] = "User updated successfully.";
            return RedirectToAction("Users");
        }

        // Edits the user's role from the user list.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string userID, string newRole)
        {
            if (string.IsNullOrEmpty(newRole) || !AllowedRoles.ContainsKey(newRole))
            {
                return RedirectToAction("Users");
            }

            var user = await _userManager.FindByIdAsync(userID);

            if (user != null)
            {
                var prevRoles = await _userManager.GetRolesAsync(user);

                if (prevRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, prevRoles);
                }

                await EnsureRoleExists(newRole);
                await _userManager.AddToRoleAsync(user, newRole);
            }

            return RedirectToAction("Users");
        }

        // View Donations - Trevor
        public IActionResult Donations()
        {
            // Retrieves all donations from the database, including the associated donation items, then orders them by donation date in descending order
            var donations = _context.Donations
                .Include(d => d.DonationItems)
                .Include(d => d.Donor)
                .OrderByDescending(d => d.DonationDate)
                .ToList();

            // Passes the list of donations to the view for display
            return View(donations);
        }

        // Approve Donations - Trevor
        [HttpPost]
        public IActionResult ApproveDonation(int id)
        {
            // Find the donation by ID
            var donation = _context.Donations.FirstOrDefault(d => d.DonationId == id);

            // If the donation is not found, return a 404 error
            if (donation == null)
            {
                return NotFound();
            }

            // Update the donation status to "Approved"
            donation.Status = "Approved";
            _context.SaveChanges();

            // Redirect back to the Donations view
            return RedirectToAction("Donations");
        }

        // Deny Donations - Trevor
        [HttpPost]
        public IActionResult DenyDonation(int id)
        {
            // Find the donation by ID
            var donation = _context.Donations.FirstOrDefault(d => d.DonationId == id);

            // If the donation is not found, return a 404 error
            if (donation == null)
            {
                return NotFound();
            }

            // Update the donation status to "Denied"
            donation.Status = "Denied";
            _context.SaveChanges();

            // Redirects back to the Donations view
            return RedirectToAction("Donations");
        }

        private static string NormalizeRole(string? role)
        {
            return role switch
            {
                "Admin" => "Admin",
                "Student" or "Students" => "Students",
                "Volunteer" or "Volunteers" => "Volunteers",
                "Donor" or "Donors" or "Vendor" or "Vendors" => "Donors",
                _ => string.Empty
            };
        }

        private static string GetRoleLabel(string? role)
        {
            var normalizedRole = NormalizeRole(role);

            return string.IsNullOrEmpty(normalizedRole)
                ? "No Role"
                : AllowedRoles[normalizedRole];
        }

        private static void PopulateRoleOptions(UserEditViewModel model)
        {
            model.AvailableRoles = AllowedRoles
                .Select(role => new SelectListItem
                {
                    Value = role.Key,
                    Text = role.Value,
                    Selected = role.Key == model.Role
                })
                .ToList();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private async Task EnsureRoleExists(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
