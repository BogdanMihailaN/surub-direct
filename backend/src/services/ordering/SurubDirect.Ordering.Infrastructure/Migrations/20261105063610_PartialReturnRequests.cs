using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartialReturnRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartialReturns",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecidedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DecidedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartialReturns_DemoOrders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "ordering",
                        principalTable: "DemoOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartialReturnLines",
                schema: "ordering",
                columns: table => new
                {
                    OfferId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductName = table.Column<string>(type: "text", nullable: false),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    OfferLabel = table.Column<string>(type: "text", nullable: false),
                    Packs = table.Column<int>(type: "integer", nullable: false),
                    UnitsPerPack = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialReturnLines", x => new { x.ReturnId, x.OfferId });
                    table.CheckConstraint("CK_PartialReturnLines_Quantity", "\"Packs\" > 0 AND \"UnitsPerPack\" > 0 AND \"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_PartialReturnLines_PartialReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "ordering",
                        principalTable: "PartialReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartialReturns_OrderId_Status",
                schema: "ordering",
                table: "PartialReturns",
                columns: new[] { "OrderId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartialReturnLines",
                schema: "ordering");

            migrationBuilder.DropTable(
                name: "PartialReturns",
                schema: "ordering");
        }
    }
}
