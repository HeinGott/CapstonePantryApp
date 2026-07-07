using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportArticleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[] { 1, "Step 1: Log in as an admin.\nStep 2: Click the Inventory button in the navbar.\nStep 3: Click the Edit button next to the item you wish to edit.\nStep 4: Change the desired fields (quantity, name, etc.) and press the Save Changes button when finished.", "item,inventory,edit", "edit-inventory-item", "Learn how to edit an item's details.", "How to Edit an Item in Inventory" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
