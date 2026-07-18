using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentPointBalancesToUserApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentPointBalance",
                table: "UserApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPointResetAt",
                table: "UserApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyPointBalance",
                table: "UserApplications",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPointBalance",
                table: "UserApplications");

            migrationBuilder.DropColumn(
                name: "LastPointResetAt",
                table: "UserApplications");

            migrationBuilder.DropColumn(
                name: "MonthlyPointBalance",
                table: "UserApplications");
        }
    }
}
