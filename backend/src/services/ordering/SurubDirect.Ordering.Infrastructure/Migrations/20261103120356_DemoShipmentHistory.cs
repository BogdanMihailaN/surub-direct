using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoShipmentHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShipmentReference",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipmentStatus",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.AddColumn<long>(
                name: "ShipmentVersion",
                schema: "ordering",
                table: "DemoOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ShipmentChanges",
                schema: "ordering",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentChanges", x => new { x.OrderId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_ShipmentChanges_DemoOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "DemoOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentChanges_OrderId_Version",
                schema: "ordering",
                table: "ShipmentChanges",
                columns: new[] { "OrderId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentChanges",
                schema: "ordering");

            migrationBuilder.DropColumn(
                name: "ShipmentReference",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "ShipmentStatus",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "ShipmentVersion",
                schema: "ordering",
                table: "DemoOrders");
        }
    }
}
