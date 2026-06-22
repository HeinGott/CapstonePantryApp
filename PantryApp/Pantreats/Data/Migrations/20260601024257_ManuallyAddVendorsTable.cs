using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManuallyAddVendorsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
        name: "Vendors",
        columns: table => new
        {
            VendorID = table.Column<int>(type: "int", nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
            PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
            Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Vendors", x => x.VendorID);
        });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
        name: "Vendors");
        }
    }
}
