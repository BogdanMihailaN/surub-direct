using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoIntegralReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnRestoredAt",
                schema: "ordering",
                table: "DemoOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnStatus",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRequested");

            migrationBuilder.AddColumn<long>(
                name: "ReturnVersion",
                schema: "ordering",
                table: "DemoOrders",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ReturnChanges",
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
                    table.PrimaryKey("PK_ReturnChanges", x => new { x.OrderId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_ReturnChanges_DemoOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "DemoOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnChanges_OrderId_Version",
                schema: "ordering",
                table: "ReturnChanges",
                columns: new[] { "OrderId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnChanges",
                schema: "ordering");

            migrationBuilder.DropColumn(
                name: "ReturnRestoredAt",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "ReturnStatus",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "ReturnVersion",
                schema: "ordering",
                table: "DemoOrders");
        }
    }
}
