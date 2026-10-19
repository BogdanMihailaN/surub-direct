using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Purchasing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    Phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupplierItems",
                schema: "purchasing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VariantLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SupplierCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PackLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UnitsPerPack = table.Column<int>(type: "integer", nullable: false),
                    NetCostPerPack = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierItems", x => x.Id);
                    table.CheckConstraint("CK_SupplierItem_Amounts", "\"UnitsPerPack\" > 0 AND \"NetCostPerPack\" > 0");
                    table.ForeignKey(
                        name: "FK_SupplierItems_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchasing",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierItems_SupplierId_SupplierCode",
                schema: "purchasing",
                table: "SupplierItems",
                columns: new[] { "SupplierId", "SupplierCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierItems_SupplierId_VariantId",
                schema: "purchasing",
                table: "SupplierItems",
                columns: new[] { "SupplierId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CountryCode_TaxId",
                schema: "purchasing",
                table: "Suppliers",
                columns: new[] { "CountryCode", "TaxId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierItems",
                schema: "purchasing");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "purchasing");
        }
    }
}
