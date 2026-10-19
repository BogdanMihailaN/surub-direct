using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;
internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.CountryCode, x.TaxId }).IsUnique();
        b.Property(x => x.Name).HasMaxLength(200); b.Property(x => x.CountryCode).HasMaxLength(2); b.Property(x => x.TaxId).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(254); b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.ContactName).HasMaxLength(160); b.Property(x => x.Address).HasMaxLength(500); b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
