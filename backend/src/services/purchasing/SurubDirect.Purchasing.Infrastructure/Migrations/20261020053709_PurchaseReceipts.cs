using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Purchasing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PurchaseReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseReceipts",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AppliedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    table.CheckConstraint("CK_PurchaseReceipt_Quantity", "\"Quantity\" > 0 AND \"Quantity\" <= 10000000");
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_PurchaseOrderLines_LineId",
                        column: x => x.LineId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrderLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_PurchaseOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "purchasing",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_LineId_Reference",
                schema: "purchasing",
                table: "PurchaseReceipts",
                columns: new[] { "LineId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_OrderId",
                schema: "purchasing",
                table: "PurchaseReceipts",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseReceipts",
                schema: "purchasing");
        }
    }
}
