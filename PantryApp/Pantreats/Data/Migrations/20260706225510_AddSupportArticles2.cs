using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportArticles2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SupportArticles",
                columns: new[] { "Id", "Content", "Keywords", "Slug", "Summary", "Title" },
                values: new object[,]
                {
                    { 2, "Step 1: Log into Pantreats or create an account.\nStep 2: Click the Application button in the navbar.\nStep 3. Fill out the required fields for the application and press the Submit button when finished.", "application,fill out", "fill-out-application", "Learn how to fill out an application for Pantreats.", "How to Fill Out an Application" },
                    { 3, "Step 1: Log in as an admin.\nStep 2: Click the User Management button in the navbar.\nStep 3: Click the dropdown menu under Actions for any user and change to the desired role.\nStep 4: Click the Save button next to the modified user when finished.", "change,user,roles", "change-user-roles", "Learn how to change the role of a user to admin, volunteerm etc.", "How to Change User Roles" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
