using Microsoft.EntityFrameworkCore;
using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Infrastructure;
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<StockAdjustment> Adjustments => Set<StockAdjustment>();
    public DbSet<StockBalance> Balances => Set<StockBalance>();
    public DbSet<StockReceipt> Receipts => Set<StockReceipt>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("inventory");
        builder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
