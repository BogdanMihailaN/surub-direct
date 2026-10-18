using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class TechnicalAttributeConfiguration : IEntityTypeConfiguration<TechnicalAttribute>
{
    public void Configure(EntityTypeBuilder<TechnicalAttribute> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
