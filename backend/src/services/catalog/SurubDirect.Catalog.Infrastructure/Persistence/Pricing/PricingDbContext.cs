using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain.Pricing;

namespace SurubDirect.Catalog.Infrastructure.Pricing;
public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<OfferPrice> Prices => Set<OfferPrice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("pricing");
        builder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly, t => t.Namespace == typeof(PricingDbContext).Namespace);
    }
}
