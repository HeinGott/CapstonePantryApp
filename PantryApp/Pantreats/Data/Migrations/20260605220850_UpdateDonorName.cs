using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDonorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Vendors]', N'U') IS NOT NULL AND OBJECT_ID(N'[Donors]', N'U') IS NULL
                BEGIN
                    EXEC sp_rename N'[Vendors]', N'Donors';
                END;
                """);

            migrationBuilder.Sql("""
                IF COL_LENGTH('Donors', 'VendorID') IS NOT NULL AND COL_LENGTH('Donors', 'DonorID') IS NULL
                BEGIN
                    EXEC sp_rename N'[Donors].[VendorID]', N'DonorID', N'COLUMN';
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Donors', 'DonorID') IS NOT NULL AND COL_LENGTH('Donors', 'VendorID') IS NULL
                BEGIN
                    EXEC sp_rename N'[Donors].[DonorID]', N'VendorID', N'COLUMN';
                END;
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Donors]', N'U') IS NOT NULL AND OBJECT_ID(N'[Vendors]', N'U') IS NULL
                BEGIN
                    EXEC sp_rename N'[Donors]', N'Vendors';
                END;
                """);
        }
    }
}
