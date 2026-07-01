using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVolunteerScheduleAndScheduleChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleChangeRequests",
                columns: table => new
                {
                    ScheduleChangeRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleChangeRequests", x => x.ScheduleChangeRequestId);
                });

            migrationBuilder.CreateTable(
                name: "VolunteerSchedules",
                columns: table => new
                {
                    VolunteerScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VolunteerSchedules", x => x.VolunteerScheduleId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VolunteerSchedules_UserId",
                table: "VolunteerSchedules",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleChangeRequests");

            migrationBuilder.DropTable(
                name: "VolunteerSchedules");
        }
    }
}
