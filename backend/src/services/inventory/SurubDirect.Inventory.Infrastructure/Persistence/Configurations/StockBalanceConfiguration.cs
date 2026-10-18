using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.HasKey(x => x.VariantId);
        builder.ToTable("Balances", table => table.HasCheckConstraint("CK_Stock_Balance", "\"OnHand\" >= \"Reserved\" AND \"Reserved\" >= 0"));
    }
}
