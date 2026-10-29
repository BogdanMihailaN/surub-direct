using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountOrderOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                schema: "ordering",
                table: "DemoOrders",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AccountLinkedAt",
                schema: "ordering",
                table: "DemoOrders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_AccountId_Sequence",
                schema: "ordering",
                table: "DemoOrders",
                columns: new[] { "AccountId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DemoOrders_AccountId_Sequence",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "ordering",
                table: "DemoOrders");

            migrationBuilder.DropColumn(
                name: "AccountLinkedAt",
                schema: "ordering",
                table: "DemoOrders");
        }
    }
}
