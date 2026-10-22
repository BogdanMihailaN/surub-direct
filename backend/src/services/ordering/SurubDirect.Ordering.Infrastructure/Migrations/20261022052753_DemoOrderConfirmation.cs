using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoOrderConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConfirmationRequested",
                schema: "ordering",
                table: "ReservationPlans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "DemoOrders",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    OwnerHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Customer = table.Column<string>(type: "jsonb", nullable: false),
                    Lines = table.Column<string>(type: "jsonb", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_OwnerHash_Sequence",
                schema: "ordering",
                table: "DemoOrders",
                columns: new[] { "OwnerHash", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoOrders_Sequence",
                schema: "ordering",
                table: "DemoOrders",
                column: "Sequence",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoOrders",
                schema: "ordering");

            migrationBuilder.DropColumn(
                name: "ConfirmationRequested",
                schema: "ordering",
                table: "ReservationPlans");
        }
    }
}
