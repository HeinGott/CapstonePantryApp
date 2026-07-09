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
                    Title = "How to Edit an Item in Inventory",
                    Slug = "edit-inventory-item",
                    Keywords = new[] { "item", "inventory", "edit" },
                    Summary = "Learn how to edit an item's details.",
                    Content = "Step 1: Log in as an admin." +
                    "\nStep 2: Click the Inventory button in the navbar." +
                    "\nStep 3: Click the Edit button next to the item you wish to edit." +
                    "\nStep 4: Change the desired fields (quantity, name, etc.) and press the Save Changes button when finished."
                },
                new SupportArticle
                {
                    Id=2,
                    Title="How to Fill Out an Application",
                    Slug="fill-out-application",
                    Keywords = new[] {"application", "fill out"},
                    Summary="Learn how to fill out an application for Pantreats.",
                    Content="Step 1: Log into Pantreats or create an account." +
                    "\nStep 2: Click the Application button in the navbar." +
                    "\nStep 3. Fill out the required fields for the application and press the Submit button when finished."
                },
                new SupportArticle
                {
                    Id=3,
                    Title="How to Change User Roles",
                    Slug="change-user-roles",
                    Keywords = new[] {"change", "user", "roles"},
                    Summary = "Learn how to change the role of a user to admin, volunteer, etc.",
                    Content="Step 1: Log in as an admin." +
                    "\nStep 2: Click the User Management button in the navbar." +
                    "\nStep 3: Click the dropdown menu under Actions for any user and change to the desired role." +
                    "\nStep 4: Click the Save button next to the modified user when finished."
                },
                new SupportArticle
                {
                    Id = 4,
                    Title = "How to Create an Account",
                    Slug = "create-account",
                    Keywords = new[] { "create", "account" },
                    Summary = "Learn how to create an account on Pantreats.",
                    Content = "Step 1: Click the Register button on the navbar." +
    "\nStep 2: From the Register page, click 'Student' or 'Donor' based on the type of account you wish to make." +
    "\nStep 3: Fill out the required fields, then press the Register button to proceed to the next page based on the role you chose to sign up with."
                },
                new SupportArticle
                {
                    Id = 5,
                    Title = "How to Make a Donation",
                    Slug = "make-donation",
                    Keywords = new[] { "donate", "donation" },
                    Summary = "Learn how to donate to Pantreats.",
                    Content = "Step 1: Log in as a donor. \nStep2: Click the 'Make a Donation' button on the dashboard." +
                    "\nStep 3: From the Make a Donation page, select the item categories you would like to donate to and enter in the desired quantity. \nStep 4: Enter a donation pickup/dropoff address and/or a comment about the donation (both of these are optional). \nStep 5: Click the 'Submit Donation' button when finished to submit your donation."
                },
                new SupportArticle
                {
                    Id = 6,
                    Title = "How to Add a Recipe",
                    Slug = "add-recipe",
                    Keywords = new[] { "recipe", "recipes" },
                    Summary = "Learn how to add a recipe to Pantreats.",
                    Content = "Step 1: Log in as an admin. \nStep 2: Click the Recipes button on the navbar. \nStep 3: From the Recipes page, click the Add Recipe button. \nStep 4: Fill out the fields (recipe title, meal type, instructions, image, dietary information, ingredients) then either press Preview Recipe to see what your recipe will look like or Save Recipe to add the recipe."
                },
                new SupportArticle
                {
                    Id = 7,
                    Title = "How to Change Accessibility Preferences",
                    Slug = "change-accessibility-preferences",
                    Keywords = new[] { "change", "accessibility" },
                    Summary = "Learn how to make your experience more accessible (have pages read to you, increase text size, etc.).",
                    Content = "Step 1: Click the 'Accessibility' button on the navbar. \nStep 2: From the Accessibility page, click any of the options located inside the cards (e.g, High Contrast) to make your experience more accessible. You may also enable the toolbar at the top of the page to access these tools from any page."
                },
                new SupportArticle
                {
                    Id = 8,
                    Title = "How to Change Your Availabilty as a Volunteer",
                    Slug = "change-volunteer-availability",
                    Keywords = new[] { "change", "volunteer", "availability" },
                    Summary = "Learn how to change your volunteering hours.",
                    Content = "Step 1: Log in as a volunteer. \nStep 2: Click the 'Schedule' button on the navbar. \nStep 3: From the My Schedule page, click the 'Request Change' button. \nStep 4: Put a check next to the new dates you are requesting, along with a short note detailing why you are requesting the change. Then press the Submit Request button. An admin will review your request when able."
                },
                new SupportArticle
                {
                    Id = 9,
                    Title = "How to Browse Recipes",
                    Slug = "browse-recipes",
                    Keywords = new[] { "browse", "recipe", "recipes" },
                    Summary = "Learn how to browse recipes on Pantreats.",
                    Content = "Step 1: From the homepage, under the 'What You Can Do With Pantreats?' section, click the 'Browse Recipes' card. \nStep 2: Enter a recipe name, ingredient, or instruction of the recipe you are looking for."
                });
        }
    }
}
