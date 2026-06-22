using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pantreats.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderFulfilmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderFulfilments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FulfilmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFulfilments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFulfilments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderFulfilments_OrderId",
                table: "OrderFulfilments",
                column: "OrderId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderFulfilments");
        }
    }
}
