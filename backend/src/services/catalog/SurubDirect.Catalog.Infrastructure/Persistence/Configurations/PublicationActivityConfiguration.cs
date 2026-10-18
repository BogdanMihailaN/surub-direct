using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace SurubDirect.Catalog.Infrastructure;
internal sealed class PublicationActivityConfiguration : IEntityTypeConfiguration<PublicationActivity>
{
    public void Configure(EntityTypeBuilder<PublicationActivity> builder)
    {
        builder.HasKey(x => x.EventId);
    }
}
