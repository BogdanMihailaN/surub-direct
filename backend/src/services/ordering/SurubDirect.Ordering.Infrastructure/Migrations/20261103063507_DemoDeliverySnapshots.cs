using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DemoDeliverySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Delivery",
                schema: "ordering",
                table: "PreparedCarts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Delivery",
                schema: "ordering",
                table: "DemoOrders",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delivery",
                schema: "ordering",
                table: "PreparedCarts");

            migrationBuilder.DropColumn(
                name: "Delivery",
                schema: "ordering",
                table: "DemoOrders");
        }
    }
}
