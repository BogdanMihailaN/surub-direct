using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Infrastructure;
public static class OrderingModule
{
    public static IServiceCollection AddOrderingPersistence(this IServiceCollection services, string connection)
    {
        services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "ordering")));
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<CreateCartHandler>();
        services.AddScoped<GetCartHandler>();
        services.AddScoped<ReplaceCartHandler>();
        services.AddScoped<SaveCartCustomerHandler>();
        services.AddScoped<IReservationPlans, ReservationPlans>();
        services.AddScoped<ProcessReservationPlanHandler>();
        services.AddScoped<PlanReservationHandler>();
        services.AddScoped<CancelReservationHandler>();
        services.AddScoped<GetReservationPlanHandler>();
        services.AddScoped<IPreparedCarts, PreparedCarts>();
        services.AddScoped<PrepareCartHandler>();
        services.AddScoped<GetPreparationHandler>();
        services.AddScoped<CancelPreparationHandler>();
        services.AddScoped<IDemoOrders, DemoOrders>();
        services.AddScoped<ConfirmDemoOrderHandler>();
        services.AddScoped<CompleteDemoOrderHandler>();
        services.AddScoped<GetDemoOrdersHandler>();
        services.AddScoped<IAdminDemoOrders, AdminDemoOrders>();
        services.AddScoped<ListAdminOrdersHandler>();
        services.AddScoped<GetAdminOrderHandler>();
        services.AddScoped<CancelDemoOrderHandler>();
        services.AddScoped<IOrderCancellations, OrderCancellations>();
        return services;
    }

    public static Task MigrateOrdering(this IServiceProvider services, CancellationToken ct = default) => services.GetRequiredService<OrderingDbContext>().Database.MigrateAsync(ct);
}
