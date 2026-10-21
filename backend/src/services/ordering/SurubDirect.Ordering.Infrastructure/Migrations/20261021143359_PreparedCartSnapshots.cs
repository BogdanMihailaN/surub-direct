using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreparedCartSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PreparedId",
                schema: "ordering",
                table: "Carts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PreparedCarts",
                schema: "ordering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CartId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceVersion = table.Column<int>(type: "integer", nullable: false),
                    Customer = table.Column<string>(type: "jsonb", nullable: false),
                    Lines = table.Column<string>(type: "jsonb", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreparedCarts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PreparedCarts_CartId_SourceVersion",
                schema: "ordering",
                table: "PreparedCarts",
                columns: new[] { "CartId", "SourceVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreparedCarts",
                schema: "ordering");

            migrationBuilder.DropColumn(
                name: "PreparedId",
                schema: "ordering",
                table: "Carts");
        }
    }
}
