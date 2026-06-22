using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApplicationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserApplications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOB = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhoneNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HouseholdBabiesToddlers = table.Column<byte>(type: "tinyint", nullable: false),
                    HouseholdBabiesChildren = table.Column<byte>(type: "tinyint", nullable: false),
                    HouseholdTeens = table.Column<byte>(type: "tinyint", nullable: false),
                    HouseholdAdults = table.Column<byte>(type: "tinyint", nullable: false),
                    HasTransportation = table.Column<bool>(type: "bit", nullable: true),
                    EmploymentStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployedHouseMembers = table.Column<byte>(type: "tinyint", nullable: false),
                    HasSNAP = table.Column<bool>(type: "bit", nullable: false),
                    HasWIC = table.Column<bool>(type: "bit", nullable: false),
                    HasTANF = table.Column<bool>(type: "bit", nullable: false),
                    IsInterestedInSNAP = table.Column<bool>(type: "bit", nullable: false),
                    IsInterestedInWIC = table.Column<bool>(type: "bit", nullable: false),
                    IsInterestedInTANF = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Campus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApplications", x => x.ApplicationId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserApplications");
        }
    }
}
