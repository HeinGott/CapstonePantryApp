using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;

namespace Pantreats.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Vendor> Vendors { get; set; } = default!;
        public DbSet<UserApplication> UserApplications { get; set; }
        public DbSet<VolunteerApplication> VolunteerApplications { get; set; }
        public DbSet<ItemRequest> ItemRequest { get; set; }
    }
}
