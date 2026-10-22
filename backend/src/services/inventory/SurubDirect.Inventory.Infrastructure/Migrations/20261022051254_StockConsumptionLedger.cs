using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockConsumptionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consumptions",
                schema: "inventory",
                columns: table => new
                {
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumptions", x => new { x.ReservationId, x.VariantId });
                    table.ForeignKey(
                        name: "FK_Consumptions_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "inventory",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consumptions_OrderId",
                schema: "inventory",
                table: "Consumptions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumptions_VariantId_CreatedAt",
                schema: "inventory",
                table: "Consumptions",
                columns: new[] { "VariantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Consumptions",
                schema: "inventory");
        }
    }
}
