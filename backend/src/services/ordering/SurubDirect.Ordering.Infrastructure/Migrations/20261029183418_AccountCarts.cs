using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AccountCarts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountId",
                schema: "ordering",
                table: "Carts",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Carts_AccountId",
                schema: "ordering",
                table: "Carts",
                column: "AccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Carts_AccountId",
                schema: "ordering",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "ordering",
                table: "Carts");
        }
    }
}
