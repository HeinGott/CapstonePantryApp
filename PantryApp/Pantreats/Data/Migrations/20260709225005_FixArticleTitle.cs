using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixArticleTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 8,
                column: "Title",
                value: "How to Change Your Availabilty as a Volunteer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 8,
                column: "Title",
                value: "How to Change Your Avalibilty as a Volunteer");
        }
    }
}
