using Microsoft.EntityFrameworkCore;
using SurubDirect.Catalog.Domain;
using SurubDirect.Catalog.Domain.Pricing;
using SurubDirect.Catalog.Infrastructure;
using SurubDirect.Catalog.Infrastructure.Pricing;
using Xunit;

namespace SurubDirect.Tests;

public sealed class CatalogPersistenceModelTests
{
    [Fact]
    public void Shared_assembly_preserves_separate_models_and_migration_histories()
    {
        const string connection = "Host=localhost;Database=model_check;Username=model_check";
        using var catalog = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "catalog")).Options);
        using var pricing = new PricingDbContext(new DbContextOptionsBuilder<PricingDbContext>()
            .UseNpgsql(connection, o => o.MigrationsHistoryTable("__EFMigrationsHistory", "pricing")).Options);

        Assert.NotNull(catalog.Model.FindEntityType(typeof(Product)));
        Assert.Null(catalog.Model.FindEntityType(typeof(OfferPrice)));
        Assert.Equal(typeof(OfferPrice), Assert.Single(pricing.Model.GetEntityTypes()).ClrType);
        Assert.All(catalog.Model.GetEntityTypes(), e => Assert.Equal("catalog", e.GetSchema()));
        Assert.All(pricing.Model.GetEntityTypes(), e => Assert.Equal("pricing", e.GetSchema()));
        Assert.False(catalog.Database.HasPendingModelChanges());
        Assert.False(pricing.Database.HasPendingModelChanges());
        Assert.Equal("20261017145727_InitialPricing", Assert.Single(pricing.Database.GetMigrations()));
        Assert.DoesNotContain("20261017145727_InitialPricing", catalog.Database.GetMigrations());
    }
}
