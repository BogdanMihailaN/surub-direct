using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReservationOutcomeOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationOutbox",
                schema: "inventory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationOutbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationOutbox_PublishedAt_OccurredAt",
                schema: "inventory",
                table: "ReservationOutbox",
                columns: new[] { "PublishedAt", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReservationOutbox_ReservationId_Status",
                schema: "inventory",
                table: "ReservationOutbox",
                columns: new[] { "ReservationId", "Status" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationOutbox",
                schema: "inventory");
        }
    }
}
