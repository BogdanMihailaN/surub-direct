using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Ordering.Application;

namespace SurubDirect.Ordering.Infrastructure;
public static class OrderingModule
{
    public static IServiceCollection AddOrderingPersistence(this IServiceCollection services, string connection)
    {
        services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "ordering")));
        services.AddScoped<IAccountCarts, AccountCarts>();
        services.AddScoped<GetAccountCartHandler>();
        services.AddScoped<CreateAccountCartHandler>();
        services.AddScoped<ReplaceAccountCartHandler>();
        services.AddScoped<SaveAccountCartCustomerHandler>();
        services.AddScoped<ImportGuestCartHandler>();
        services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
        services.AddScoped<IExpiredCartCleanup, ExpiredCartCleanup>();
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
        services.AddScoped<IAccountOrders, AccountOrders>();
        services.AddScoped<LinkAccountOrderHandler>();
        services.AddScoped<GetAccountOrdersHandler>();
        services.AddScoped<ConfirmDemoOrderHandler>();
        services.AddScoped<CompleteDemoOrderHandler>();
        services.AddScoped<GetDemoOrdersHandler>();
        services.AddScoped<IAdminDemoOrders, AdminDemoOrders>();
        services.AddScoped<ListAdminOrdersHandler>();
        services.AddScoped<GetAdminOrderHandler>();
        services.AddScoped<CancelDemoOrderHandler>();
        services.AddScoped<ReservationInbox>();
        services.AddScoped<FailedMessageStore>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IFailedMessageRetention, FailedMessageRetention>();
        services.AddScoped<IMessagingAdministration, MessagingAdministration>();
        services.AddScoped<IMessageFailureMonitor, MessageFailureMonitor>();
        services.AddScoped<GetMessagingStatusHandler>();
        services.AddScoped<ReplayMessageHandler>();
        services.AddScoped<IOrderCancellations, OrderCancellations>();
        return services;
    }

    public static Task MigrateOrdering(this IServiceProvider services, CancellationToken ct = default) => services.GetRequiredService<OrderingDbContext>().Database.MigrateAsync(ct);
}
