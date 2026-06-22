using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Vendors",
                table: "Vendors");

            migrationBuilder.RenameTable(
                name: "Vendors",
                newName: "Donors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Donors",
                table: "Donors",
                column: "DonorID");

            migrationBuilder.CreateTable(
                name: "Donations",
                columns: table => new
                {
                    DonationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonorId = table.Column<int>(type: "int", nullable: false),
                    DonationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Donations", x => x.DonationId);
                });

            migrationBuilder.CreateTable(
                name: "DonationItems",
                columns: table => new
                {
                    DonationItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonationId = table.Column<int>(type: "int", nullable: false),
                    ItemName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonationItems", x => x.DonationItemId);
                    table.ForeignKey(
                        name: "FK_DonationItems_Donations_DonationId",
                        column: x => x.DonationId,
                        principalTable: "Donations",
                        principalColumn: "DonationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonationItems_DonationId",
                table: "DonationItems",
                column: "DonationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonationItems");

            migrationBuilder.DropTable(
                name: "Donations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Donors",
                table: "Donors");

            migrationBuilder.RenameTable(
                name: "Donors",
                newName: "Vendors");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Vendors",
                table: "Vendors",
                column: "DonorID");
        }
    }
}
