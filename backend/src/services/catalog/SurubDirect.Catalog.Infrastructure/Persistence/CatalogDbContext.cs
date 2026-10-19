using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain;

namespace SurubDirect.Catalog.Infrastructure;
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<TechnicalAttribute> TechnicalAttributes => Set<TechnicalAttribute>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<VariantAttributeValue> VariantAttributeValues => Set<VariantAttributeValue>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Variant> Variants => Set<Variant>();
    public DbSet<SalesOffer> Offers => Set<SalesOffer>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<PublicationActivity> Activities => Set<PublicationActivity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("catalog");
        builder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
