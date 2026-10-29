using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class ShoppingCartConfiguration : IEntityTypeConfiguration<ShoppingCart>
{
    public void Configure(EntityTypeBuilder<ShoppingCart> b)
    {
        b.ToTable("Carts");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.OwnerHash).HasMaxLength(64);
        b.Property(c => c.AccountId).HasMaxLength(450);
        b.HasIndex(c => c.AccountId).IsUnique();
        b.HasIndex(c => c.OwnerHash).IsUnique();
        b.HasIndex(c => c.ExpiresAt);
        b.Property(c => c.Version).IsConcurrencyToken();
        b.Property(c => c.Customer).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<CustomerDraft>(v, (JsonSerializerOptions? )null));
        b.Property(c => c.Lines).HasColumnType("jsonb").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions? )null), v => JsonSerializer.Deserialize<List<CartSelection>>(v, (JsonSerializerOptions? )null)!).Metadata.SetValueComparer(new ValueComparer<List<CartSelection>>((a, b) => a!.SequenceEqual(b!), v => v.Aggregate(0, (h, x) => HashCode.Combine(h, x.GetHashCode())), v => v.ToList()));
    }
}
