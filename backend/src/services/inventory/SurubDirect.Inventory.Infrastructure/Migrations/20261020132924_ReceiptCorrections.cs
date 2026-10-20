using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReceiptCorrections",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptCorrections", x => x.Id);
                    table.CheckConstraint("CK_ReceiptCorrection_Quantity", "\"Quantity\" BETWEEN 1 AND 10000000");
                    table.ForeignKey(
                        name: "FK_ReceiptCorrections_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalSchema: "inventory",
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptCorrections_ReceiptId_Reference",
                schema: "inventory",
                table: "ReceiptCorrections",
                columns: new[] { "ReceiptId", "Reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptCorrections_VariantId_CreatedAt",
                schema: "inventory",
                table: "ReceiptCorrections",
                columns: new[] { "VariantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptCorrections",
                schema: "inventory");
        }
    }
}
