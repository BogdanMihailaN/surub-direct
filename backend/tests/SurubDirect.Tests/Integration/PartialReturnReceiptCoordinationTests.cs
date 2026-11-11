using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Domain;
using SurubDirect.Ordering.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnReceiptCoordinationTests(OrderingCartFixture ordering, DatabaseFixture inventory) : IClassFixture<OrderingCartFixture>, IClassFixture<DatabaseFixture>
{
    private async Task<PartialReturnRequest> Seed()
    {
        var (order, plan) = ReturnSamples.Create();
        await using var scope = ordering.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        db.DemoOrders.Add(order); db.ReservationPlans.Add(plan); await db.SaveChangesAsync();
        var requests = scope.ServiceProvider.GetRequiredService<IPartialReturns>();
        var request = await requests.Request(order.Id, Guid.NewGuid(), [new(order.Lines[0].OfferId, 1)], "Retur produse", "admin", default);
        await requests.Decide(order.Id, request.Id, Guid.NewGuid(), 0, "Approved", "Aprobare retur", "admin", default);
        await scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>().Receive(order.Id, request.Id, Guid.NewGuid(), 1, "Produse primite", "admin", true, default);
        await using var stockScope = inventory.Services.CreateAsyncScope();
        var stock = stockScope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IInventoryRepository>();
        var variant = order.Lines[0].VariantId;
        await stock.Receive(SurubDirect.Inventory.Domain.StockReceipt.Create(Guid.NewGuid(), variant, 100, "INITIAL", "test"), default);
        var reservations = stockScope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IStockReservations>();
        await reservations.Reserve(SurubDirect.Inventory.Domain.StockReservation.Create(order.Id, order.Id, 120, [new(variant, 20)], DateTimeOffset.UtcNow), default);
        await reservations.Consume(order.Id, order.Id, default);
        return (await requests.Read(order.Id, default)).Single();
    }
    private async Task<DateTimeOffset> Remote(PartialReturnRequest request, CancellationToken ct)
    {
        await using var scope = inventory.Services.CreateAsyncScope();
        var receipt = SurubDirect.Inventory.Domain.ReturnReceipt.Create(request.Id, request.OrderId, request.OrderId,
            request.Lines.GroupBy(x => x.VariantId).Select(g => new SurubDirect.Inventory.Domain.ReturnReceiptLine(g.Key, g.Sum(x => x.Quantity))), DateTimeOffset.UtcNow);
        return (await scope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IReturnReceipts>().Receive(receipt, ct)).CreatedAt;
    }
    private async Task<int> Process(Func<PartialReturnRequest, CancellationToken, Task<DateTimeOffset>> remote)
    {
        await using var scope = ordering.Services.CreateAsyncScope();
        return await new ProcessPartialReturnReceiptHandler(scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>(), new TestPartialReturnInventory(remote), TimeProvider.System).Handle(default);
    }
    private async Task<PartialReturnRequest> Read(PartialReturnRequest request)
    {
        await using var scope = ordering.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<IPartialReturns>().Read(request.OrderId, default)).Single();
    }
    private async Task Due(PartialReturnRequest request)
    {
        await using var scope = ordering.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().PartialReturns.Where(x => x.Id == request.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(v => v.NextReceiptAttemptAt, DateTimeOffset.UtcNow.AddMinutes(-1)));
    }
    [Fact]
    public async Task Lost_inventory_response_retries_same_receipt_without_duplicate_stock()
    {
        var r = await Seed();
        Assert.Equal(1, await Process(async (request, ct) => { await Remote(request, ct); throw new HttpRequestException("Lost response with private details"); }));
        var waiting = await Read(r);
        Assert.Equal("Restoring", waiting.Status); Assert.Null(waiting.InventoryReceivedAt);
        Assert.Equal(1, waiting.ReceiptAttempts); Assert.DoesNotContain("private", waiting.ReceiptError);
        Assert.Equal(0, await Process(Remote));
        await Due(r);
        Assert.Equal(1, await Process(Remote));
        var complete = await Read(r);
        Assert.Equal("Restored", complete.Status); Assert.Equal(2, complete.ReceiptAttempts);
        Assert.Null(complete.ReceiptError); Assert.Null(complete.NextReceiptAttemptAt);
        Assert.Equal(0, await Process(Remote));
        await using var stockScope = inventory.Services.CreateAsyncScope();
        var ledger = await stockScope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IStockLedger>().Read(r.Lines[0].VariantId, 1, default);
        Assert.Equal(90, ledger.OnHand); Assert.Single(ledger.Items, x => x.Kind == "ReturnReceipt");
        await using var scope = ordering.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>().Receive(r.OrderId, r.Id, r.ReceiptOperationId!.Value, 1, "Produse primite", "admin", true, default);
        await scope.ServiceProvider.GetRequiredService<IPartialReturns>().Decide(r.OrderId, r.Id, r.DecisionId!.Value, 0, "Approved", "Aprobare retur", "admin", default);
        Assert.Equal(complete.InventoryReceivedAt, (await Read(r)).InventoryReceivedAt);
        var plan = await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().ReservationPlans.SingleAsync(x => x.Id == r.OrderId);
        Assert.Equal("Consumed", plan.Status); Assert.False(plan.RestorationRequested);
    }
    [Fact]
    public async Task Local_failure_after_inventory_commit_leaves_intent_retryable()
    {
        var r = await Seed();
        await using (var scope = ordering.Services.CreateAsyncScope())
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>().Process(async (request, ct) =>
            { request.ConfirmReceipt(await Remote(request, ct)); throw new InvalidOperationException("Local failure before commit"); }, default));
        Assert.Equal("Restoring", (await Read(r)).Status);
        Assert.Equal(1, await Process(Remote));
        Assert.Equal("Restored", (await Read(r)).Status);
        await using var stockScope = inventory.Services.CreateAsyncScope();
        var ledger = await stockScope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IStockLedger>().Read(r.Lines[0].VariantId, 1, default);
        Assert.Equal(90, ledger.OnHand); Assert.Single(ledger.Items, x => x.Kind == "ReturnReceipt");
    }
    [Fact]
    public async Task Concurrent_workers_complete_one_pending_receipt_once()
    {
        var r = await Seed(); var calls = 0;
        async Task<DateTimeOffset> Receive(PartialReturnRequest request, CancellationToken ct) { Interlocked.Increment(ref calls); return await Remote(request, ct); }
        var results = await Task.WhenAll(Process(Receive), Process(Receive));
        Assert.Equal(1, results.Sum()); Assert.Equal(1, calls);
        Assert.Equal("Restored", (await Read(r)).Status);
    }
    [Fact]
    public async Task Unavailable_inventory_preserves_intent_and_schedules_retry()
    {
        var r = await Seed();
        await Process((_, _) => throw new HttpRequestException("503"));
        var waiting = await Read(r);
        Assert.Equal("Restoring", waiting.Status); Assert.NotNull(waiting.ReceiptError); Assert.NotNull(waiting.NextReceiptAttemptAt);
        await Due(r); await Process(Remote);
        Assert.Equal("Restored", (await Read(r)).Status);
    }
    [Fact]
    public async Task Manual_retry_is_audited_idempotent_and_recovers_lost_remote_response_once()
    {
        var r = await Seed();
        await Process(async (request, ct) => { await Remote(request, ct); throw new HttpRequestException("Lost response"); });
        var operationId = Guid.NewGuid();
        async Task Retry(long version = 2, string reason = "Inventory verificat")
        {
            await using var scope = ordering.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>().Retry(r.OrderId, r.Id, operationId, version, reason, "admin", default);
        }
        await Task.WhenAll(Retry(), Retry());
        var pending = await Read(r);
        Assert.Single(pending.ReceiptRetries); Assert.Equal(3, pending.Version);
        Assert.Equal(r.ReceiptOperationId, pending.ReceiptOperationId);
        Assert.Equal(r.Lines, pending.Lines);
        Assert.Equal(1, pending.ReceiptAttempts);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => Retry(reason: "Alt motiv"))).Status);
        await using (var scope = ordering.Services.CreateAsyncScope())
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>()
                .Retry(r.OrderId, r.Id, Guid.NewGuid(), 3, "Prea devreme", "admin", default))).Status);
        Assert.Equal(1, await Process(Remote));
        await Retry();
        var complete = await Read(r);
        Assert.Equal("Restored", complete.Status); Assert.Single(complete.ReceiptRetries);
        Assert.False(complete.ReceiptNeedsAttention);
        await using var stockScope = inventory.Services.CreateAsyncScope();
        var ledger = await stockScope.ServiceProvider.GetRequiredService<SurubDirect.Inventory.Application.IStockLedger>().Read(r.Lines[0].VariantId, 1, default);
        Assert.Equal(90, ledger.OnHand); Assert.Single(ledger.Items, x => x.Kind == "ReturnReceipt");
    }
    [Fact]
    public async Task Receipt_alert_appears_after_three_failures_and_clears_only_after_confirmation()
    {
        var r = await Seed();
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await Due(r);
            await Process((_, _) => throw new HttpRequestException("Unavailable"));
            Assert.Equal(attempt >= 3, (await Read(r)).ReceiptNeedsAttention);
        }
        await using (var scope = ordering.Services.CreateAsyncScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IPartialReturnReceipts>();
            Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => repo.Retry(Guid.NewGuid(), r.Id, Guid.NewGuid(), 2, "Verificare", "admin", default))).Status);
            Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => repo.Retry(r.OrderId, r.Id, Guid.NewGuid(), 1, "Verificare", "admin", default))).Status);
            await repo.Retry(r.OrderId, r.Id, Guid.NewGuid(), 2, "Verificare", "admin", default);
        }
        Assert.True((await Read(r)).ReceiptNeedsAttention);
        await Process(Remote);
        Assert.False((await Read(r)).ReceiptNeedsAttention);
    }

}
