using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StockAdjustments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Adjustments",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ExpectedOnHand = table.Column<long>(type: "bigint", nullable: false),
                    ExpectedReserved = table.Column<long>(type: "bigint", nullable: false),
                    Reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjustments", x => x.Id);
                    table.CheckConstraint("CK_Adjustment_Quantity", "\"Quantity\" <> 0 AND \"Quantity\" BETWEEN -10000000 AND 10000000 AND \"ExpectedReserved\" >= 0 AND \"ExpectedOnHand\" >= \"ExpectedReserved\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adjustments_VariantId_CreatedAt",
                schema: "inventory",
                table: "Adjustments",
                columns: new[] { "VariantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adjustments",
                schema: "inventory");
        }
    }
}
