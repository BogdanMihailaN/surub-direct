using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PartialReturnRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PartialRefundTotal",
                schema: "ordering",
                table: "PaymentSimulations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PartialReturnId",
                schema: "ordering",
                table: "PaymentSimulationEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                schema: "ordering",
                table: "PaymentSimulationEvents",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentSimulationEvents_OrderId_PartialReturnId",
                schema: "ordering",
                table: "PaymentSimulationEvents",
                columns: new[] { "OrderId", "PartialReturnId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentSimulationEvents_OrderId_PartialReturnId",
                schema: "ordering",
                table: "PaymentSimulationEvents");

            migrationBuilder.DropColumn(
                name: "PartialRefundTotal",
                schema: "ordering",
                table: "PaymentSimulations");

            migrationBuilder.DropColumn(
                name: "PartialReturnId",
                schema: "ordering",
                table: "PaymentSimulationEvents");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                schema: "ordering",
                table: "PaymentSimulationEvents");
        }
    }
}
