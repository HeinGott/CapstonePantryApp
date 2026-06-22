using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Pantreats.Models;

namespace Pantreats.Controllers //Claude used for assistance
{
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> ProfilePicture(string userId)
        {
            var filePath = "";

            foreach (var ext in new[] { ".jpg", ".png", ".jpeg", ".gif"})
            {
                var path = Path.Combine(@"C:\PantreatsUploads\profiles", userId + ext);
                if(System.IO.File.Exists(path))
                {
                    filePath = path;
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "image/jpeg");
        }
    }
}
