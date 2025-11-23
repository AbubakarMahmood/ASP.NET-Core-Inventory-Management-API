using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryAPI.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add RowVersion column to Users table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Add RowVersion column to Products table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Add RowVersion column to WorkOrders table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkOrders",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Add RowVersion column to WorkOrderItems table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "WorkOrderItems",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Add RowVersion column to StockMovements table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StockMovements",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            // Add RowVersion column to FilterPresets table
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "FilterPresets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove RowVersion column from Users table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            // Remove RowVersion column from Products table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            // Remove RowVersion column from WorkOrders table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkOrders");

            // Remove RowVersion column from WorkOrderItems table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "WorkOrderItems");

            // Remove RowVersion column from StockMovements table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StockMovements");

            // Remove RowVersion column from FilterPresets table
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "FilterPresets");
        }
    }
}
