using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.Catalog.Application.Pricing;
using SurubDirect.Catalog.Contracts.Pricing;

namespace SurubDirect.Catalog.Infrastructure.Pricing;
public static class PricingModule
{
    public static IServiceCollection AddPricing(this IServiceCollection s, string connection)
    {
        s.AddDbContext<PricingDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__EFMigrationsHistory", "pricing")));
        s.AddScoped<PriceRepository>();
        s.AddScoped<IPriceRepository>(p => p.GetRequiredService<PriceRepository>());
        s.AddScoped<IPricingReader>(p => p.GetRequiredService<PriceRepository>());
        s.AddScoped<SetOfferPriceHandler>();
        return s;
    }

    public static Task MigratePricing(this IServiceProvider s, CancellationToken ct = default) => s.GetRequiredService<PricingDbContext>().Database.MigrateAsync(ct);
}
