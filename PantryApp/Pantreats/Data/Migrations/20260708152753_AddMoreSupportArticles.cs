using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSupportArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[,]
                {
                    { 4, "Step 1: Click the Register button on the navbar.\nStep 2: From the Register page, click 'Student' or 'Donor' based on the type of account you wish to make.\nStep 3: Fill out the required fields, then press the Register button to proceed to the next page based on the role you chose to sign up with.", "create,account", "create-account", "Learn how to create an account on Pantreats.", "How to Create an Account" },
                    { 5, "Step 1: Log in as a donor. \nStep2: Click the 'Make a Donation' button on the dashboard.\nStep 3: From the Make a Donation page, select the item categories you would like to donate to and enter in the desired quantity. \nStep 4: Enter a donation pickup/dropoff address and/or a comment about the donation (both of these are optional). \nStep 5: Click the 'Submit Donation' button when finished to submit your donation.", "donate,donation", "make-donation", "Learn how to donate to Pantreats.", "How to Make a Donation" },
                    { 6, "Step 1: Log in as an admin. \nStep 2: Click the Recipes button on the navbar. \nStep 3: From the Recipes page, click the Add Recipe button. \nStep 4: Fill out the fields (recipe title, meal type, instructions, image, dietary information, ingredients) then either press Preview Recipe to see what your recipe will look like or Save Recipe to add the recipe.", "recipe,recipes", "add-recipe", "Learn how to add a recipe to Pantreats.", "How to Add a Recipe" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
