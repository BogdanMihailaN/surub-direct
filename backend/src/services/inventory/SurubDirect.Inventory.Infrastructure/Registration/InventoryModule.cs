using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Inventory.Application;
using SurubDirect.Inventory.Contracts;

namespace SurubDirect.Inventory.Infrastructure;

public static class InventoryModule
{
    public static IServiceCollection AddInventory(this IServiceCollection s, string connection)
    {
        s.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "inventory")));
        s.AddScoped<InventoryRepository>();
        s.AddScoped<IInventoryRepository>(p => p.GetRequiredService<InventoryRepository>());
        s.AddScoped<IInventoryReader>(p => p.GetRequiredService<InventoryRepository>());
        s.AddScoped<ReceiveStockHandler>();
        s.AddScoped<IStockLedger, StockLedger>();
        s.AddScoped<AdjustStockHandler>();
        s.AddScoped<IReceiptCorrections, ReceiptCorrections>();
        s.AddScoped<CorrectReceiptHandler>();
        s.AddScoped<GetStockLedgerHandler>();
        return s;
    }

    public static Task MigrateInventory(this IServiceProvider s, CancellationToken ct = default) => s.GetRequiredService<InventoryDbContext>().Database.MigrateAsync(ct);
}
