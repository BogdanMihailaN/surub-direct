using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Purchasing.Infrastructure;

public static class PurchasingModule
{
    public static IServiceCollection AddPurchasing(this IServiceCollection services, string connection)
    {
        services.AddDbContext<PurchasingDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "purchasing")));
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<CreateSupplierHandler>();
        services.AddScoped<UpdateSupplierHandler>();
        services.AddScoped<AddSupplierItemHandler>();
        services.AddScoped<UpdateSupplierItemHandler>();
        services.AddScoped<GetSupplierHandler>();
        services.AddScoped<ListSuppliersHandler>();
        services.AddScoped<SearchVariantsHandler>();
        services.AddScoped<IPurchaseCorrectionRepository, PurchaseCorrectionRepository>();
        services.AddScoped<CorrectPurchaseReceiptHandler>();
        services.AddScoped<RetryPurchaseCorrectionHandler>();
        services.AddScoped<IPurchaseReceiptRepository, PurchaseReceiptRepository>();
        services.AddScoped<ReceivePurchaseOrderHandler>();
        services.AddScoped<RetryPurchaseReceiptHandler>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<CreatePurchaseOrderHandler>();
        services.AddScoped<UpdatePurchaseOrderHandler>();
        services.AddScoped<AddPurchaseOrderLineHandler>();
        services.AddScoped<UpdatePurchaseOrderLineHandler>();
        services.AddScoped<RemovePurchaseOrderLineHandler>();
        services.AddScoped<MarkPurchaseOrderSentHandler>();
        services.AddScoped<CancelPurchaseOrderHandler>();
        services.AddScoped<GetPurchaseOrderHandler>();
        services.AddScoped<ListPurchaseOrdersHandler>();
        return services;
    }
    public static Task MigratePurchasing(this IServiceProvider services, CancellationToken ct = default) => services.GetRequiredService<PurchasingDbContext>().Database.MigrateAsync(ct);
}
