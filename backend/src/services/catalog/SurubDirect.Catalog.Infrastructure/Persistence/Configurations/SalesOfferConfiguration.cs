using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class SalesOfferConfiguration : IEntityTypeConfiguration<SalesOffer>
{
    public void Configure(EntityTypeBuilder<SalesOffer> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.ToTable("Offers", table => table.HasCheckConstraint("CK_Offer_Quantities", "\"UnitsPerPack\" > 0 AND \"MinPacks\" > 0 AND \"PackStep\" > 0"));
    }
}
