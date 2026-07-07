using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTypoInArticleConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Summary",
                value: "Learn how to change the role of a user to admin, volunteer, etc.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SupportArticles",
                keyColumn: "Id",
                keyValue: 3,
                column: "Summary",
                value: "Learn how to change the role of a user to admin, volunteerm etc.");
        }
    }
}
