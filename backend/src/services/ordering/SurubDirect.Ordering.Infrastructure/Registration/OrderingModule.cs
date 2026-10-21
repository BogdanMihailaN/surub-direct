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
        return services;
    }

    public static Task MigrateOrdering(this IServiceProvider services, CancellationToken ct = default) => services.GetRequiredService<OrderingDbContext>().Database.MigrateAsync(ct);
}
