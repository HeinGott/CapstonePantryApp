using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617053000_EnsureUserApplicationsTableExists")]
    public class EnsureUserApplicationsTableExists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[UserApplications]', N'U') IS NULL
                BEGIN
                    CREATE TABLE [UserApplications] (
                        [ApplicationId] int NOT NULL IDENTITY,
                        [UserId] nvarchar(max) NOT NULL,
                        [StudentId] int NOT NULL,
                        [RegistrationDate] datetime2 NOT NULL,
                        [FirstName] nvarchar(max) NOT NULL,
                        [MiddleName] nvarchar(max) NOT NULL,
                        [LastName] nvarchar(max) NOT NULL,
                        [DOB] datetime2 NULL,
                        [PhoneNum] nvarchar(max) NOT NULL,
                        [Gender] nvarchar(max) NOT NULL,
                        [StudentStatus] nvarchar(max) NOT NULL,
                        [HouseholdBabiesToddlers] tinyint NOT NULL,
                        [HouseholdBabiesChildren] tinyint NOT NULL,
                        [HouseholdTeens] tinyint NOT NULL,
                        [HouseholdAdults] tinyint NOT NULL,
                        [HasTransportation] bit NULL,
                        [EmploymentStatus] nvarchar(max) NOT NULL,
                        [EmployedHouseMembers] tinyint NOT NULL,
                        [HasSNAP] bit NOT NULL,
                        [HasWIC] bit NOT NULL,
                        [HasTANF] bit NOT NULL,
                        [IsInterestedInSNAP] bit NOT NULL,
                        [IsInterestedInWIC] bit NOT NULL,
                        [IsInterestedInTANF] bit NOT NULL,
                        [IsActive] bit NOT NULL,
                        [Campus] nvarchar(max) NOT NULL,
                        CONSTRAINT [PK_UserApplications] PRIMARY KEY ([ApplicationId])
                    );
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[UserApplications]', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE [UserApplications];
                END
                """);
        }
    }
}
