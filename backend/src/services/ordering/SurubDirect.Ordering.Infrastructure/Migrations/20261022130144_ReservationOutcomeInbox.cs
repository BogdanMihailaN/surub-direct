using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReservationOutcomeInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReservationInbox",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationInbox", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReservationInbox",
                schema: "ordering");
        }
    }
}
