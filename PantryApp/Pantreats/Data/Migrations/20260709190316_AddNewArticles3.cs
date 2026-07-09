using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewArticles3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[,]
                {
                    { 7, "Step 1: Click the 'Accessibility' button on the navbar. \nStep 2: From the Accessibility page, click any of the options located inside the cards (e.g, High Contrast) to make your experience more accessible. You may also enable the toolbar at the top of the page to access these tools from any page.", "change,accessibility", "change-accessibility-preferences", "Learn how to make your experience more accessible (have pages read to you, increase text size, etc.).", "How to Change Accessibility Preferences" },
                    { 8, "Step 1: Log in as a volunteer. \nStep 2: Click the 'Schedule' button on the navbar. \nStep 3: From the My Schedule page, click the 'Request Change' button. \nStep 4: Put a check next to the new dates you are requesting, along with a short note detailing why you are requesting the change. Then press the Submit Request button. An admin will review your request when able.", "change,volunteer,availability", "change-volunteer-availability", "Learn how to change your volunteering hours.", "How to Change Your Avalibilty as a Volunteer" },
                    { 9, "Step 1: From the homepage, under the 'What You Can Do With Pantreats?' section, click the 'Browse Recipes' card. \nStep 2: Enter a recipe name, ingredient, or instruction of the recipe you are looking for.", "browse,recipe,recipes", "browse-recipes", "Learn how to browse recipes on Pantreats.", "How to Browse Recipes" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
