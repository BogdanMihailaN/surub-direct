using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartialReturnReceiptRetries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartialReturnReceiptRetries",
                schema: "ordering",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartialReturnReceiptRetries", x => new { x.ReturnId, x.OperationId });
                    table.ForeignKey(
                        name: "FK_PartialReturnReceiptRetries_PartialReturns_ReturnId",
                        column: x => x.ReturnId,
                        principalSchema: "ordering",
                        principalTable: "PartialReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartialReturnReceiptRetries",
                schema: "ordering");
        }
    }
}
