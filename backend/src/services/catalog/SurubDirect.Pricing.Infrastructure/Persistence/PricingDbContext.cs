using Microsoft.EntityFrameworkCore;
using SurubDirect.Pricing.Domain;

namespace SurubDirect.Pricing.Infrastructure;
public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public DbSet<OfferPrice> Prices => Set<OfferPrice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("pricing");
        builder.ApplyConfigurationsFromAssembly(typeof(PricingDbContext).Assembly);
    }
}
