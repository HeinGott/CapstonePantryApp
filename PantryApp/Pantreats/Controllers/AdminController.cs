using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Pantreats.Models;
using Microsoft.EntityFrameworkCore;
using Pantreats.Data;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager)
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

                model.Add(new UserManagerViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return View(model);
        }


        
        //Edits the users role -Jorge
        [HttpPost]
        public async Task<IActionResult> EditUser(string userID, string newRole)
        {
            //If you save the Template one it redirects and does give you none
            if (string.IsNullOrEmpty(newRole))
            {
                return RedirectToAction("Users");
            } 

            var user = await _userManager.FindByIdAsync(userID);

            if (user != null)
            {
                //Gets previous role
                var prevRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

                //Makes sure role is not null
                if (prevRole != null)
                {
                    //Remove role
                    await _userManager.RemoveFromRoleAsync(user, prevRole);
                }
                
                //Add New role
                await _userManager.AddToRoleAsync(user, newRole);
            }
            //Sends Back to Main page after
            return RedirectToAction("Users");
        }

        // View Donations - Trevor
        public IActionResult Donations()
        {
            // Retrieves all donations from the database, including the associated donation items, then orders them by donation date in descending order
            var donations = _context.Donations
                .Include(d => d.DonationItems)
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

    }
}
