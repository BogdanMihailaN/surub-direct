using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain.Pricing;

namespace SurubDirect.Catalog.Infrastructure.Pricing;
internal sealed class OfferPriceConfiguration : IEntityTypeConfiguration<OfferPrice>
{
    public void Configure(EntityTypeBuilder<OfferPrice> builder)
    {
        builder.HasKey(x => x.OfferId);
        builder.Ignore(x => x.GrossAmount);
        builder.Property(x => x.NetAmount).HasPrecision(18, 6);
        builder.Property(x => x.TaxRate).HasPrecision(5, 4);
        builder.ToTable("Prices", table => table.HasCheckConstraint("CK_Price_Amounts", "\"NetAmount\" > 0 AND \"TaxRate\" >= 0 AND \"TaxRate\" <= 1"));
    }
}
