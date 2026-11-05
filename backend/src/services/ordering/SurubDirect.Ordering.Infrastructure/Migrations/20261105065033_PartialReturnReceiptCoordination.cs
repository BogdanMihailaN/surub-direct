using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartialReturnReceiptCoordination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "InventoryReceivedAt",
                schema: "ordering",
                table: "PartialReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextReceiptAttemptAt",
                schema: "ordering",
                table: "PartialReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceiptAttempts",
                schema: "ordering",
                table: "PartialReturns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptError",
                schema: "ordering",
                table: "PartialReturns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceiptOperationId",
                schema: "ordering",
                table: "PartialReturns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptReason",
                schema: "ordering",
                table: "PartialReturns",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReceiptRequestedAt",
                schema: "ordering",
                table: "PartialReturns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceivedBy",
                schema: "ordering",
                table: "PartialReturns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartialReturns_Status_NextReceiptAttemptAt",
                schema: "ordering",
                table: "PartialReturns",
                columns: new[] { "Status", "NextReceiptAttemptAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PartialReturns_Status_NextReceiptAttemptAt",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "InventoryReceivedAt",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "NextReceiptAttemptAt",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceiptAttempts",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceiptError",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceiptOperationId",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceiptReason",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceiptRequestedAt",
                schema: "ordering",
                table: "PartialReturns");

            migrationBuilder.DropColumn(
                name: "ReceivedBy",
                schema: "ordering",
                table: "PartialReturns");
        }
    }
}
