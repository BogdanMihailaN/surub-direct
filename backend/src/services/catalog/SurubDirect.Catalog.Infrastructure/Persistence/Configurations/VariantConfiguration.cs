using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasMany(x => x.AttributeValues).WithOne().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.HasMany(x => x.Offers).WithOne().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.DiameterMm).HasPrecision(12, 3);
        builder.Property(x => x.LengthMm).HasPrecision(12, 3);
    }
}
