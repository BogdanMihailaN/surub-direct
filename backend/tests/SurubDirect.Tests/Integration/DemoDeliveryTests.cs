using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
using SurubDirect.Ordering.Infrastructure.Delivery;

namespace SurubDirect.Tests;
public sealed class DemoDeliveryTests(ReservationCoordinatorFixture fixture) : IClassFixture<ReservationCoordinatorFixture>
{
    [Theory]
    [InlineData(false, "demo-standard", 20, 16.53)]
    [InlineData(true, "demo-express", 30, 24.79)]
    public async Task Delivery_is_revalidated_and_preserved_in_preparation_order_and_admin(bool account, string code, decimal gross, decimal net)
    {
        var owner = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var accountId = account ? Guid.NewGuid().ToString() : null;
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var carts = s.GetRequiredService<IShoppingCartRepository>();
        var accounts = s.GetRequiredService<IAccountCarts>();
        var catalog = new PreparationCatalog();
        var cart = account ? await accounts.Create(accountId!, default) : await carts.Create(owner, default);
        var selection = new CartSelection[] { new("surub", catalog.First, 2), new("surub", catalog.Second, 3) };
        cart = account ? await accounts.Replace(accountId!, new(cart.Id, cart.Version, selection), default)
            : await carts.Replace(owner, cart.Id, cart.Version, selection, default);
        cart = account ? await accounts.SaveCustomer(accountId!, new(cart.Id, cart.Version, CustomerDraftSamples.Person()), default)
            : await carts.SaveCustomer(owner, cart.Id, cart.Version, CustomerDraftSamples.Person(), default);
        var prepared = s.GetRequiredService<IPreparedCarts>();
        var command = new PrepareCart(Guid.NewGuid(), cart.Id, cart.Version, code);
        IDeliveryRates rates = new TestDeliveryRates();
        Task<SurubDirect.Ordering.Contracts.PreparedCartDto> Prepare(PrepareCart request) => account
            ? new PrepareAccountCartHandler(accounts, prepared, new(catalog), rates).Handle(accountId!, request, default)
            : new PrepareCartHandler(carts, prepared, new(catalog), rates).Handle(owner, request, default);
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => Prepare(command with { DeliveryCode = "unknown" }))).Status);
        var snapshot = await Prepare(command);
        Assert.Equal(42.35m + gross, snapshot.GrossTotal);
        Assert.Equal(35m + net, snapshot.NetTotal);
        Assert.Equal(code, snapshot.Delivery!.Code);
        rates = new ConfiguredDeliveryRates([]);
        Assert.Equal(snapshot.Delivery, (await Prepare(command)).Delivery);
        rates = new ConfiguredDeliveryRates([new() { Code = code, Label = "Tarif modificat", NetAmount = net + 5, GrossAmount = gross + 10 }]);
        Assert.Equal(gross + 10, Assert.Single(new GetDeliveryOptionsHandler(rates).Handle()).GrossAmount);
        Assert.Equal(snapshot.Delivery, (await Prepare(command)).Delivery);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Prepare(command with { DeliveryCode = null }))).Status);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        if (account) await s.GetRequiredService<ConfirmAccountOrderHandler>().Handle(accountId!, command.RequestId, default);
        else await s.GetRequiredService<IDemoOrders>().Confirm(owner, command.RequestId, default);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        var admin = (await s.GetRequiredService<IAdminDemoOrders>().Find(command.RequestId, default))!;
        Assert.Equal(snapshot.GrossTotal, admin.Order.GrossTotal);
        Assert.Equal(snapshot.Delivery, admin.Order.Delivery);
        var list = await s.GetRequiredService<IAdminDemoOrders>().Read(null, long.Parse(admin.Order.Number[5..]), 1, default);
        Assert.Equal(snapshot.GrossTotal, Assert.Single(list.Items).GrossTotal);
        if (account) await s.GetRequiredService<CompleteAccountOrderHandler>().Handle(accountId!, command.RequestId, default);
        else await s.GetRequiredService<IDemoOrders>().Complete(owner, command.RequestId, default);
        var db = s.GetRequiredService<OrderingDbContext>();
        Assert.False(await db.PreparedCarts.AnyAsync(x => x.Id == command.RequestId));
        Assert.Equal(code, (await db.DemoOrders.AsNoTracking().SingleAsync(x => x.Id == command.RequestId)).Delivery!.Code);
        await s.GetRequiredService<IOrderCancellations>().Cancel(command.RequestId, "Test restituire", "admin", default);
        await s.GetRequiredService<ProcessReservationPlanHandler>().Handle(default);
        var cancelled = (await s.GetRequiredService<IAdminDemoOrders>().Find(command.RequestId, default))!;
        Assert.Equal("Cancelled", cancelled.Order.Status);
        Assert.Equal(snapshot.GrossTotal, cancelled.Order.GrossTotal);
        Assert.Equal(snapshot.Delivery, cancelled.Order.Delivery);
    }
}
