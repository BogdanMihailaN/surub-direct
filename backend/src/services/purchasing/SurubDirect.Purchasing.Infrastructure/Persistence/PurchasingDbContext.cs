using Microsoft.EntityFrameworkCore;
using SurubDirect.Purchasing.Domain;
namespace SurubDirect.Purchasing.Infrastructure;

public sealed class PurchasingDbContext(DbContextOptions<PurchasingDbContext> options) : DbContext(options)
{
    public DbSet<PurchaseReceiptCorrection> ReceiptCorrections => Set<PurchaseReceiptCorrection>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierItem> SupplierItems => Set<SupplierItem>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("purchasing");
        builder.ApplyConfigurationsFromAssembly(typeof(PurchasingDbContext).Assembly);
    }
}
