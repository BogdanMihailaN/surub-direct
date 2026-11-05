using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartialReturnReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnReceipts",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnReceipts_Reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "inventory",
                        principalTable: "Reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReturnReceiptLines",
                schema: "inventory",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnReceiptLines", x => new { x.ReceiptId, x.VariantId });
                    table.CheckConstraint("CK_ReturnReceiptLines_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_ReturnReceiptLines_ReturnReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "inventory",
                        principalTable: "ReturnReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceiptLines_VariantId",
                schema: "inventory",
                table: "ReturnReceiptLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnReceipts_ReservationId",
                schema: "inventory",
                table: "ReturnReceipts",
                column: "ReservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnReceiptLines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "ReturnReceipts",
                schema: "inventory");
        }
    }
}
