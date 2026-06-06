using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Pantreats.Models;

namespace Pantreats.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;


        public AdminController(UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager)
        {
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


    }
}
