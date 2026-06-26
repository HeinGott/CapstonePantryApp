using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSourceForKiosk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Orders', 'OrderSource') IS NULL
                BEGIN
                    ALTER TABLE [Orders] ADD [OrderSource] nvarchar(max) NOT NULL DEFAULT N'';
                END;

                UPDATE [Orders]
                SET [OrderSource] = 'Online'
                WHERE [OrderSource] IS NULL OR LTRIM(RTRIM([OrderSource])) = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('Orders', 'OrderSource') IS NOT NULL
                BEGIN
                    ALTER TABLE [Orders] DROP COLUMN [OrderSource];
                END;
                """);
        }
    }
}
