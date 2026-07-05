using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedSupportArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[,]
                {
                    { 1, "Step 1: Click the Inventory button in the navbar.\nStep 2: Click the Edit button next to the item you wish to edit.\nStep 3: Change the desired fields (quantity, name, etc.) and press the Save Changes button when finished.", "item,inventory,edit", "edit-inventory-item", "Learn how to edit an item's details.", "How to Edit an Item in Inventory (Admin)" },
                    { 2, "Step 1: Log into Pantreats or create an account.\nStep 2: Click the Application button in the navbar.\nStep 3: Fill out the required fields for the application and press the Submit button when finished.", "application,fill out", "fill-out-application", "Learn how to fill out an application for Pantreats.", "How to Fill Out an Application" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
