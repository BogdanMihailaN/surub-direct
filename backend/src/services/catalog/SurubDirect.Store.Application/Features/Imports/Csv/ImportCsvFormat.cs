namespace SurubDirect.Store.Application.Imports;
public static class ImportCsvFormat
{
    public const int MaximumBytes = 1_048_576;
    public const int MaximumRows = 500;
    public const string Header = "product_name;product_slug;category_slug;description;sku;variant_label;diameter_mm;length_mm;material;finish;offer_code;offer_label;units_per_pack;min_packs;pack_step;net_price;tax_rate";
    public const string Template = Header + "\r\n" +
        "Șurub demonstrativ;surub-demonstrativ;inlocuieste-cu-slug-categorie;Exemplu ilustrativ de înlocuit;DEMO-M6-20;M6 x 20;6;20;Oțel;Zincat;DEMO-M6-20-P100;Cutie 100 bucăți;100;1;1;10.50;0.21\r\n";
}
