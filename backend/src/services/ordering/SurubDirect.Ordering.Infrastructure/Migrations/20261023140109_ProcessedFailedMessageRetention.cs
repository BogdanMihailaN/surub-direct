using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProcessedFailedMessageRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                schema: "ordering",
                table: "FailedReservationMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                schema: "ordering",
                table: "FailedReservationMessages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PayloadRemovedAt",
                schema: "ordering",
                table: "FailedReservationMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE ordering."FailedReservationMessages"
                SET "PayloadHash" = upper(encode(sha256("Payload"), 'hex')),
                    "EventId" = CASE WHEN "CanReplay" AND pg_input_is_valid("MessageId", 'uuid')
                        THEN "MessageId"::uuid ELSE NULL END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FailedReservationMessages_PublishedAt_Id",
                schema: "ordering",
                table: "FailedReservationMessages",
                columns: new[] { "PublishedAt", "Id" },
                filter: "\"PublishedAt\" IS NOT NULL AND \"PayloadRemovedAt\" IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FailedReservationMessages_RemovedPayload",
                schema: "ordering",
                table: "FailedReservationMessages",
                sql: "\"PayloadRemovedAt\" IS NULL OR (\"PublishedAt\" IS NOT NULL AND \"RequestedAt\" IS NOT NULL AND \"ReplayRequestId\" IS NOT NULL AND \"EventId\" IS NOT NULL AND \"PayloadHash\" IS NOT NULL AND octet_length(\"Payload\") = 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FailedReservationMessages_PublishedAt_Id",
                schema: "ordering",
                table: "FailedReservationMessages");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FailedReservationMessages_RemovedPayload",
                schema: "ordering",
                table: "FailedReservationMessages");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "ordering",
                table: "FailedReservationMessages");

            migrationBuilder.DropColumn(
                name: "PayloadHash",
                schema: "ordering",
                table: "FailedReservationMessages");

            migrationBuilder.DropColumn(
                name: "PayloadRemovedAt",
                schema: "ordering",
                table: "FailedReservationMessages");
        }
    }
}
