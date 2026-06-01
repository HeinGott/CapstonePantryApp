using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;

namespace Pantreats.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Vendor> Vendors { get; set; } = default!;
        public DbSet<UserApplication> UserApplications { get; set; }
        public DbSet<VolunteerApplication> VolunteerApplications { get; set; }
        public DbSet<ItemRequest> ItemRequest { get; set; }
        public DbSet<OrderFulfilment> OrderFulfilments { get; set; }

        /*this method configures the relationships between OrderItem and Inventory,
        this will ensure if the inventory item is deleted, that the upc will be set to null and
        the order items will still be in the history*/
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vendor>().ToTable("Vendors");

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Inventory)
                .WithMany()
                .HasForeignKey(oi => oi.InventoryUPC)
                .OnDelete(DeleteBehavior.SetNull);

            //configures the relationships between Orders and OrderFulfilment
            modelBuilder.Entity<OrderFulfilment>()
                .HasOne(of => of.Order)
                .WithOne(of => of.OrderFulfilment)
                .HasForeignKey<OrderFulfilment>(of => of.OrderId);

        }
    }
}