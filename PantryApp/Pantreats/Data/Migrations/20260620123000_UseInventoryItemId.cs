using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Pantreats.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260620123000_UseInventoryItemId")]
    public partial class UseInventoryItemId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryImages_Inventory_UPC",
                table: "InventoryImages");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Inventory_InventoryUPC",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryImages",
                table: "InventoryImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_InventoryUPC",
                table: "OrderItems");

            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "Inventory",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<int>(
                name: "InventoryItemId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryItemId",
                table: "InventoryImages",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE oi
                SET oi.InventoryItemId = i.ItemId
                FROM OrderItems oi
                INNER JOIN Inventory i ON oi.InventoryUPC = i.UPC
                """);

            migrationBuilder.Sql("""
                UPDATE ii
                SET ii.InventoryItemId = i.ItemId
                FROM InventoryImages ii
                INNER JOIN Inventory i ON ii.UPC = i.UPC
                """);

            migrationBuilder.AlterColumn<string>(
                name: "InventoryUPC",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InventoryItemId",
                table: "InventoryImages",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "UPC",
                table: "InventoryImages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "ItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryImages",
                table: "InventoryImages",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_InventoryItemId",
                table: "OrderItems",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_UPC",
                table: "Inventory",
                column: "UPC",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryImages_Inventory_InventoryItemId",
                table: "InventoryImages",
                column: "InventoryItemId",
                principalTable: "Inventory",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Inventory_InventoryItemId",
                table: "OrderItems",
                column: "InventoryItemId",
                principalTable: "Inventory",
                principalColumn: "ItemId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryImages_Inventory_InventoryItemId",
                table: "InventoryImages");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Inventory_InventoryItemId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InventoryImages",
                table: "InventoryImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_InventoryItemId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Inventory_UPC",
                table: "Inventory");

            migrationBuilder.AddColumn<string>(
                name: "UPC",
                table: "InventoryImages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ii
                SET ii.UPC = i.UPC
                FROM InventoryImages ii
                INNER JOIN Inventory i ON ii.InventoryItemId = i.ItemId
                """);

            migrationBuilder.AlterColumn<string>(
                name: "UPC",
                table: "InventoryImages",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InventoryUPC",
                table: "OrderItems",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "InventoryImages");

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "Inventory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inventory",
                table: "Inventory",
                column: "UPC");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InventoryImages",
                table: "InventoryImages",
                column: "UPC");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_InventoryUPC",
                table: "OrderItems",
                column: "InventoryUPC");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryImages_Inventory_UPC",
                table: "InventoryImages",
                column: "UPC",
                principalTable: "Inventory",
                principalColumn: "UPC",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Inventory_InventoryUPC",
                table: "OrderItems",
                column: "InventoryUPC",
                principalTable: "Inventory",
                principalColumn: "UPC",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
