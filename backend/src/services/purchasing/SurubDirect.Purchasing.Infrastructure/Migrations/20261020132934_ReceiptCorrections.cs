using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Purchasing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptCorrections",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptCorrections", x => x.Id);
                    table.CheckConstraint("CK_Correction_Quantity", "\"Quantity\" BETWEEN 1 AND 10000000");
                    table.ForeignKey(
                        name: "FK_ReceiptCorrections_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReceiptCorrections_PurchaseReceipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptCorrections_OrderId",
                schema: "purchasing",
                table: "ReceiptCorrections",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptCorrections_ReceiptId_Reference",
                schema: "purchasing",
                table: "ReceiptCorrections",
                columns: new[] { "ReceiptId", "Reference" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptCorrections",
                schema: "purchasing");
        }
    }
}
