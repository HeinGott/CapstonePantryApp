using Microsoft.EntityFrameworkCore;
using Pantreats.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace Pantreats.Data
{
    public class SupportArticleConfiguration : IEntityTypeConfiguration<SupportArticle>
    {
        public void Configure(EntityTypeBuilder<SupportArticle> builder)
        {

            builder
                .Property(a => a.Keywords)
                .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

            builder.HasData(
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
                   Keywords = new[] { "application", "fill out" },
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
