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
        public DbSet<SupportArticle> SupportArticles { get; set; }

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

            modelBuilder.Entity<SupportArticle>()
                .Property(a => a.Keywords)
                .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

            modelBuilder.Entity<SupportArticle>().HasData(
                new SupportArticle
                {
                    Id = 1,
                    Title = "How to Edit an Item in Inventory (Admin)",
                    Slug = "edit-inventory-item",
                    Keywords = new[] { "item", "inventory", "edit" },
                    Summary = "Learn how to edit an item's details.",
                    Content = "Step 1: Click the Inventory button in the navbar." +
                    "\nStep 2: Click the Edit button next to the item you wish to edit." + "\nStep 3: Change the desired fields (quantity, name, etc.) and press the Save Changes button when finished."
                },
               new SupportArticle
               {
                   Id = 2,
                   Title = "How to Fill Out an Application",
                   Slug = "fill-out-application",
                   Keywords = new[] { "application", "fill out"},
                   Summary = "Learn how to fill out an application for Pantreats.",
                   Content = "Step 1: Log into Pantreats or create an account." +
                   "\nStep 2: Click the Application button in the navbar." +
                   "\nStep 3: Fill out the required fields for the application and press the Submit button when finished."
               },
               new SupportArticle
               {
                   Id = 3,
                   Title = "How to Change User Roles",
                   Slug = "change-user-roles",
                   Keywords = new[] { "change", "user roles", "user", "roles" },
                   Summary = "Learn how to change the role of a user to admin, volunteer, etc.",
                   Content = "Step 1: Log in as an admin." +
                   "\nStep 2: Click the User Management button in the navbar." +
                   "\nStep 3. Click the dropdown menu under Actions for any user and change to the desired role." +
                   "\nStep 4. Click the Save button next to the modified user when finished."
               });
        }
    }
}
