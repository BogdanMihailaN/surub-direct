using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;

namespace SurubDirect.Tests;
public sealed class AdminDemoOrderTests(OrderingCartFixture fixture) : IClassFixture<OrderingCartFixture>
{
    [Fact]
    public async Task Admin_reads_paginated_filtered_snapshots_across_owners_and_consistent_confirmation_details()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var ids = new List<Guid>();
        for (var i = 0; i < 28; i++)
        {
            var now = DateTimeOffset.UtcNow;
            var cart = ShoppingCart.Create(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)), now);
            var offer = Guid.NewGuid();
            var variant = Guid.NewGuid();
            var id = Guid.NewGuid();
            ids.Add(id);
            cart.Replace(cart.Id, cart.Version, [new("demo", offer, 2)], now);
            cart.SaveCustomer(cart.Id, cart.Version, CustomerDraftSamples.Person(), now);
            var snapshot = PreparedCart.Create(id, cart, [new(offer, variant, "Produs salvat", "SKU", "Cutie", 2, 10, 10, 12.1m, 20, 24.2m)], "RON", now);
            var plan = ReservationPlan.Create(id, id, [new(variant, 20)], now);
            plan.Observe("Active", now.AddMinutes(15), now);
            plan.Confirm(now);
            var order = DemoOrder.Create(cart.OwnerHash, snapshot, now);
            if (i < 27)
            {
                plan.Observe(i % 2 == 0 ? "Consumed" : "Expired", now.AddMinutes(15), now);
                order.Reconcile(plan, now);
            }

            db.DemoOrders.Add(order);
            db.ReservationPlans.Add(plan);
        }

        await db.SaveChangesAsync();
        var list = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        var first = await list.Handle(null, null, 1, default);
        var second = await list.Handle(null, null, 2, default);
        Assert.Equal(28, first.Total);
        Assert.Equal(25, first.Items.Count);
        Assert.Equal(3, second.Items.Count);
        Assert.Empty(first.Items.Select(x => x.Id).Intersect(second.Items.Select(x => x.Id)));
        var all = first.Items.Concat(second.Items).ToArray();
        Assert.Equal(ids.Order(),all.Select(x=>x.Id).Order());
        var sequences = all.Select(x=>long.Parse(x.Number[5..])).ToArray();
        Assert.Equal(sequences.OrderDescending(),sequences);
        var confirmed = await list.Handle("Confirmed", null, 1, default);
        Assert.Equal(14, confirmed.Total);
        Assert.All(confirmed.Items, x => Assert.Equal("Confirmed", x.Status));
        var exact = await list.Handle("Confirmed", confirmed.Items[0].Number.ToLowerInvariant(), 1, default);
        Assert.Equal(confirmed.Items[0], Assert.Single(exact.Items));
        Assert.Empty((await list.Handle("Failed", confirmed.Items[0].Number, 1, default)).Items);
        var detail = await scope.ServiceProvider.GetRequiredService<GetAdminOrderHandler>().Handle(confirmed.Items[0].Id, default);
        Assert.Equal("Consumed", detail.Reservation.Status);
        Assert.Equal("Confirmed", detail.Order.Status);
        Assert.Equal(24.2m, detail.Order.GrossTotal);
        Assert.Equal("Produs salvat", Assert.Single(detail.Order.Lines).ProductName);
        Assert.Equal(CustomerDraftSamples.Person().FullName, detail.Order.Customer.FullName);
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<GetAdminOrderHandler>().Handle(Guid.NewGuid(), default))).Status);
        Assert.Empty((await list.Handle(null, null, 100, default)).Items);
    }

    [Theory]
    [InlineData("invalid", null, 1)]
    [InlineData(null, "%", 1)]
    [InlineData(null, "DEMO-0", 1)]
    [InlineData(null, "999999999999999999999999", 1)]
    [InlineData(null, null, 0)]
    [InlineData(null, null, 100001)]
    public async Task Invalid_admin_filters_are_rejected(string? status, string? number, int page)
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ListAdminOrdersHandler>();
        Assert.Equal(400, (await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(status, number, page, default))).Status);
    }
}
