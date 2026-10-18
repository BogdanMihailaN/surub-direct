using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class VariantAttributeValueConfiguration : IEntityTypeConfiguration<VariantAttributeValue>
{
    public void Configure(EntityTypeBuilder<VariantAttributeValue> builder)
    {
        builder.HasKey(x => new { x.VariantId, x.AttributeId });
        builder.Property(x => x.NumberValue).HasPrecision(18, 6);
        builder.HasOne<TechnicalAttribute>().WithMany().HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AttributeId, x.NumberValue });
        builder.HasIndex(x => new { x.AttributeId, x.NormalizedText });
        builder.ToTable("VariantAttributeValues", table => table.HasCheckConstraint("CK_Attribute_Value", "(\"TextValue\" IS NOT NULL AND \"NumberValue\" IS NULL) OR (\"TextValue\" IS NULL AND \"NumberValue\" IS NOT NULL)"));
    }
}
