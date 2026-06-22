using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VolunteerApplications",
                columns: table => new
                {
                    VolunteerApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasVolunteeredBefore = table.Column<bool>(type: "bit", nullable: false),
                    PreviousCapacity = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReasonForVolunteering = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VolunteerFrequency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtherFrequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MonMorning = table.Column<bool>(type: "bit", nullable: false),
                    MonAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    TueMorning = table.Column<bool>(type: "bit", nullable: false),
                    TueAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    WedMorning = table.Column<bool>(type: "bit", nullable: false),
                    WedAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    ThuMorning = table.Column<bool>(type: "bit", nullable: false),
                    ThuAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    FriMorning = table.Column<bool>(type: "bit", nullable: false),
                    FriAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    SatMorning = table.Column<bool>(type: "bit", nullable: false),
                    SatAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    SunMorning = table.Column<bool>(type: "bit", nullable: false),
                    SunAfternoon = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerApplications", x => x.VolunteerApplicationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VolunteerApplications");
        }
    }
}
