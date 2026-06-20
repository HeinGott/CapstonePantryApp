using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pantreats.Models;

namespace Pantreats.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Donation> Donations { get; set; }
        public DbSet<DonationItem> DonationItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<Donor> Donors { get; set; } = default!;
        public DbSet<UserApplication> UserApplications { get; set; }
        public DbSet<VolunteerApplication> VolunteerApplications { get; set; }
        public DbSet<ItemRequest> ItemRequest { get; set; }
        public DbSet<OrderFulfilment> OrderFulfilments { get; set; }
        public DbSet<InventoryImage> InventoryImages { get; set; }

        /*this method configures the relationships between OrderItem and Inventory,
        this will ensure if the inventory item is deleted, that the upc will be set to null and
        the order items will still be in the history*/
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Donor>().ToTable("Donors");

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Inventory)
                .WithMany()
                .HasForeignKey(oi => oi.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            //set all the points in inventory to have a default value of 1
            modelBuilder.Entity<Inventory>()
                .Property(i => i.Points)
                .HasDefaultValue(1);

            modelBuilder.Entity<Inventory>()
                .HasIndex(i => i.UPC)
                .IsUnique();

            //configures the relationships between Orders and OrderFulfilment
            modelBuilder.Entity<OrderFulfilment>()
                .HasOne(of => of.Order)
                .WithOne(of => of.OrderFulfilment)
                .HasForeignKey<OrderFulfilment>(of => of.OrderId);


            modelBuilder.Entity<InventoryImage>(entity =>
            {
                entity.HasKey(e => e.InventoryItemId);

                entity.Property(e => e.ImageData)
                      .HasColumnType("varbinary(max)");
                //configures the relationships between InventoryImage and Inventory
                entity.HasOne(e => e.Inventory)
                    .WithOne(i => i.InventoryImage)
                    .HasForeignKey<InventoryImage>(e => e.InventoryItemId);

            });
        }
    }
}
