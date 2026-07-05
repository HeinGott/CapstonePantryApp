using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRoleChangeInstruction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[] { 3, "Step 1: Log in as an admin.\nStep 2: Click the User Management button in the navbar.\nStep 3. Click the dropdown menu under Actions for any user and change to the desired role.\nStep 4. Click the Save button next to the modified user when finished.", "change,user roles,user,roles", "change-user-roles", "Learn how to change the role of a user to admin, volunteer, etc.", "How to Change User Roles" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
