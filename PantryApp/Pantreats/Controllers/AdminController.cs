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
        
    }
}
