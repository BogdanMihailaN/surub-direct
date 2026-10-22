using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoOrderCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RestorationRequested",
                schema: "ordering",
                table: "ReservationPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancellationRequestedAt",
                schema: "ordering",
                table: "DemoOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                schema: "ordering",
                table: "DemoOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RestorationRequested",
                schema: "ordering",
                table: "ReservationPlans");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAt",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                schema: "ordering",
                table: "DemoOrders");
        }
    }
}
