using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FailedMessageRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailedReservationMessages",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    MessageType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PreviousReplayId = table.Column<Guid>(type: "uuid", nullable: true),
                    Payload = table.Column<byte[]>(type: "bytea", nullable: false),
                    OriginalSize = table.Column<int>(type: "integer", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    CanReplay = table.Column<bool>(type: "boolean", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReplayRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReplayReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailedReservationMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailedReservationMessages_Fingerprint",
                schema: "ordering",
                table: "FailedReservationMessages",
                column: "Fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FailedReservationMessages_PublishedAt_RequestedAt",
                schema: "ordering",
                table: "FailedReservationMessages",
                columns: new[] { "PublishedAt", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FailedReservationMessages_ReplayRequestId",
                schema: "ordering",
                table: "FailedReservationMessages",
                column: "ReplayRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailedReservationMessages",
                schema: "ordering");
        }
    }
}
