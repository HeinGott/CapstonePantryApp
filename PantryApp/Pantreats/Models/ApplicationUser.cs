using Microsoft.AspNetCore.Identity;
namespace Pantreats.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ImagePath { get; set; }
    }
}
