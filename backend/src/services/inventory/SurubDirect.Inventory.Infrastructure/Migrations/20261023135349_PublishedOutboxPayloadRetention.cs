using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublishedOutboxPayloadRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                schema: "inventory",
                table: "ReservationOutbox",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PayloadRemovedAt",
                schema: "inventory",
                table: "ReservationOutbox",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationOutbox_PublishedAt_Id",
                schema: "inventory",
                table: "ReservationOutbox",
                columns: new[] { "PublishedAt", "Id" },
                filter: "\"PublishedAt\" IS NOT NULL AND \"PayloadRemovedAt\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ReservationOutbox_RemovedPayload",
                schema: "inventory",
                table: "ReservationOutbox",
                sql: "\"PayloadRemovedAt\" IS NULL OR (\"PublishedAt\" IS NOT NULL AND \"PayloadHash\" IS NOT NULL AND \"Payload\" = '{}'::jsonb)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReservationOutbox_PublishedAt_Id",
                schema: "inventory",
                table: "ReservationOutbox");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ReservationOutbox_RemovedPayload",
                schema: "inventory",
                table: "ReservationOutbox");

            migrationBuilder.DropColumn(
                name: "PayloadHash",
                schema: "inventory",
                table: "ReservationOutbox");

            migrationBuilder.DropColumn(
                name: "PayloadRemovedAt",
                schema: "inventory",
                table: "ReservationOutbox");
        }
    }
}
