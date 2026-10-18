using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockReceiptConfiguration : IEntityTypeConfiguration<StockReceipt>
{
    public void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        builder.HasIndex(x => new { x.VariantId, x.CreatedAt });
        builder.ToTable("Receipts", table => table.HasCheckConstraint("CK_Receipt_Quantity", "\"Quantity\" > 0"));
    }
}
