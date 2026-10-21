using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class PreparedCartConfiguration : IEntityTypeConfiguration<PreparedCart>
{
    public void Configure(EntityTypeBuilder<PreparedCart> b)
    {
        b.ToTable("PreparedCarts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.CartId, x.SourceVersion }).IsUnique();
        b.Property(x => x.Currency).HasMaxLength(10);
        b.Property(x => x.Customer).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<CustomerDraft>(v, (JsonSerializerOptions? )null)!);
        b.Property(x => x.Lines).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<List<PreparedCartLine>>(v, (JsonSerializerOptions? )null)!).Metadata.SetValueComparer(new ValueComparer<List<PreparedCartLine>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
    }
}
