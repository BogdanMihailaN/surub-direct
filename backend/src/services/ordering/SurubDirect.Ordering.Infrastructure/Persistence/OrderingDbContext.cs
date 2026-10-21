using Microsoft.EntityFrameworkCore;
using SurubDirect.Ordering.Domain;

namespace SurubDirect.Ordering.Infrastructure;
public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("ordering");
        b.ApplyConfigurationsFromAssembly(typeof(OrderingDbContext).Assembly);
    }
}
