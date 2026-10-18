using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SurubDirect.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StructuredTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                schema: "catalog",
                table: "Products",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categories_Categories_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalAttributes",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoryAttributes",
                schema: "catalog",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAttributes", x => new { x.CategoryId, x.AttributeId });
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_TechnicalAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalSchema: "catalog",
                        principalTable: "TechnicalAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VariantAttributeValues",
                schema: "catalog",
                columns: table => new
                {
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    NormalizedText = table.Column<string>(type: "text", nullable: true),
                    NumberValue = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantAttributeValues", x => new { x.VariantId, x.AttributeId });
                    table.CheckConstraint("CK_Attribute_Value", "(\"TextValue\" IS NOT NULL AND \"NumberValue\" IS NULL) OR (\"TextValue\" IS NULL AND \"NumberValue\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_VariantAttributeValues_TechnicalAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalSchema: "catalog",
                        principalTable: "TechnicalAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VariantAttributeValues_Variants_VariantId",
                        column: x => x.VariantId,
                        principalSchema: "catalog",
                        principalTable: "Variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                schema: "catalog",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NormalizedName",
                schema: "catalog",
                table: "Categories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                schema: "catalog",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                schema: "catalog",
                table: "Categories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_AttributeId",
                schema: "catalog",
                table: "CategoryAttributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalAttributes_Code",
                schema: "catalog",
                table: "TechnicalAttributes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_AttributeId_NormalizedText",
                schema: "catalog",
                table: "VariantAttributeValues",
                columns: new[] { "AttributeId", "NormalizedText" });

            migrationBuilder.CreateIndex(
                name: "IX_VariantAttributeValues_AttributeId_NumberValue",
                schema: "catalog",
                table: "VariantAttributeValues",
                columns: new[] { "AttributeId", "NumberValue" });


            // Preserve every legacy category label and link existing products before adding the FK.
            migrationBuilder.Sql("""
                INSERT INTO catalog."Categories" ("Id", "Name", "NormalizedName", "Slug", "ParentId", "SortOrder")
                SELECT md5('category:' || upper(name))::uuid, name, upper(name),
                    coalesce(nullif(trim(both '-' from regexp_replace(translate(lower(name), 'ăâîșşțţ', 'aaisstt'), '[^a-z0-9]+', '-', 'g')), ''), 'categorie') || '-' || substr(md5(upper(name)), 1, 8), NULL, 0
                FROM (
                    SELECT min(label) AS name FROM (
                        SELECT "Category" AS label FROM catalog."Products"
                        UNION ALL SELECT unnest(ARRAY['Șuruburi','Șuruburi metrice','Șuruburi pentru lemn','Șuruburi autoperforante','Șuruburi gips-carton','Piulițe','Șaibe','Dibluri','Ancore','Tije filetate','Nituri','Piulițe nituibile'])
                    ) labels GROUP BY upper(label)
                ) names;
                UPDATE catalog."Categories" SET "ParentId" = md5('category:' || upper('Șuruburi'))::uuid
                    WHERE "Name" IN ('Șuruburi metrice','Șuruburi pentru lemn','Șuruburi autoperforante','Șuruburi gips-carton');
                UPDATE catalog."Products" p SET "CategoryId" = c."Id" FROM catalog."Categories" c WHERE upper(p."Category") = c."NormalizedName";
                INSERT INTO catalog."TechnicalAttributes" ("Id", "Code", "Name", "Kind", "Source", "Unit", "SortOrder") VALUES
                    (md5('attribute:diametru')::uuid, 'diametru', 'Diametru', 1, 1, 'mm', 10),
                    (md5('attribute:lungime')::uuid, 'lungime', 'Lungime', 1, 2, 'mm', 20),
                    (md5('attribute:material')::uuid, 'material', 'Material', 0, 3, NULL, 30),
                    (md5('attribute:finisaj')::uuid, 'finisaj', 'Finisaj', 0, 4, NULL, 40),
                    (md5('attribute:filet')::uuid, 'filet', 'Tip filet', 0, 0, NULL, 50),
                    (md5('attribute:standard')::uuid, 'standard', 'Standard DIN / ISO', 0, 0, NULL, 60),
                    (md5('attribute:clasa-rezistenta')::uuid, 'clasa-rezistenta', 'Clasă de rezistență', 0, 0, NULL, 70),
                    (md5('attribute:pas-filet')::uuid, 'pas-filet', 'Pas filet', 1, 0, 'mm', 80);
                INSERT INTO catalog."CategoryAttributes" ("CategoryId", "AttributeId")
                    SELECT c."Id", a."Id" FROM catalog."Categories" c CROSS JOIN catalog."TechnicalAttributes" a WHERE a."Source" <> 0;
                INSERT INTO catalog."CategoryAttributes" ("CategoryId", "AttributeId")
                    SELECT c."Id", a."Id" FROM catalog."Categories" c CROSS JOIN catalog."TechnicalAttributes" a
                    WHERE c."Name" IN ('Șuruburi','Piulițe','Tije filetate') AND a."Source" = 0;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products",
                column: "CategoryId",
                principalSchema: "catalog",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropTable(
                name: "CategoryAttributes",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "VariantAttributeValues",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "TechnicalAttributes",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                schema: "catalog",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                schema: "catalog",
                table: "Products");
        }
    }
}
