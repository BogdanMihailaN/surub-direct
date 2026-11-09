using Microsoft.EntityFrameworkCore;
using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Application;
using SurubDirect.Ordering.Contracts;

namespace SurubDirect.Ordering.Infrastructure;
internal sealed class AdminDemoOrders(OrderingDbContext db) : IAdminDemoOrders
{
    public async Task<AdminOrderPageDto> Read(string? status, long? sequence, int page, CancellationToken ct, string? returnStatus = null, string? paymentStatus = null, string? partialReturnStatus = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var query = from order in db.DemoOrders.AsNoTracking()
                    join payment in db.PaymentSimulations.AsNoTracking() on order.Id equals payment.OrderId into payments
                    from payment in payments.DefaultIfEmpty()
                    select new { Order = order, PaymentStatus = payment == null ? "NotStarted" : payment.Status };
        if (status is not null)
            query = query.Where(x => x.Order.Status == status);
        if (returnStatus is not null)
            query = query.Where(x => x.Order.ReturnStatus == returnStatus);
        if (paymentStatus is not null)
            query = query.Where(x => x.PaymentStatus == paymentStatus);
        if (partialReturnStatus is not null)
            query = query.Where(x => db.PartialReturns.Any(r => r.OrderId == x.Order.Id && r.Status == partialReturnStatus));
        if (sequence is not null)
            query = query.Where(x => x.Order.Sequence == sequence);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.Order.Sequence).Skip((page - 1) * 25).Take(25).ToListAsync(ct);
        var pageIds = items.Select(x => x.Order.Id).ToArray();
        var pending = partialReturnStatus == "Restoring"
            ? await db.PartialReturns.AsNoTracking().Where(x => pageIds.Contains(x.OrderId) && x.Status == "Restoring")
                .OrderBy(x => x.ReceiptRequestedAt).ThenBy(x => x.Id)
                .Select(x => new { x.OrderId, Receipt = new PendingReceiptDto(x.Id, x.ReceiptRequestedAt, x.NextReceiptAttemptAt, x.ReceiptAttempts) })
                .ToListAsync(ct)
            : [];
        var pendingByOrder = pending.ToLookup(x => x.OrderId, x => x.Receipt);
        await tx.CommitAsync(ct);
        return new(items.Select(x => new AdminOrderSummaryDto(x.Order.Id, $"DEMO-{x.Order.Sequence:D8}", x.Order.Status, x.Order.Customer.FullName, x.Order.Currency, x.Order.Lines.Sum(l => l.GrossTotal) + (x.Order.Delivery?.GrossAmount ?? 0), x.Order.CreatedAt, x.Order.ReturnStatus, x.PaymentStatus, pendingByOrder[x.Order.Id].ToArray())).ToArray(), total, page, 25);
    }

    public async Task<AdminOrderDetailDto?> Find(Guid id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        var order = await db.DemoOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null)
            return null;
        var plan = await db.ReservationPlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BusinessException("Starea confirmării nu este disponibilă.", 503);
        var history = await db.OrderProcessingChanges.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Version)
            .Select(x => new OrderProcessingChangeDto(x.OperationId, x.Version, x.FromStatus, x.ToStatus, x.Reason, x.Actor, x.CreatedAt)).ToListAsync(ct);
        var payment = await db.PaymentSimulations.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id, ct);
        var paymentHistory = await db.PaymentSimulationEvents.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Version)
            .Select(x => new PaymentSimulationEventDto(x.OperationId, x.AttemptId, x.Version, x.Action, x.FromStatus, x.ToStatus, x.Reason, x.Actor, x.CreatedAt, x.PartialReturnId, x.RefundAmount)).ToListAsync(ct);
        var shipmentHistory = await db.ShipmentChanges.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Version)
            .Select(x => new ShipmentChangeDto(x.OperationId, x.Version, x.FromStatus, x.ToStatus, x.Reason, x.Actor, x.CreatedAt)).ToListAsync(ct);
        var returnHistory = await db.ReturnChanges.AsNoTracking().Where(x => x.OrderId == id).OrderBy(x => x.Version)
            .Select(x => new ReturnChangeDto(x.OperationId, x.Version, x.FromStatus, x.ToStatus, x.Reason, x.Actor, x.CreatedAt)).ToListAsync(ct);
        var orderReturn = new OrderReturnDto(order.ReturnStatus, order.ReturnVersion, order.ReturnRestoredAt, returnHistory);
        var shipment = new ShipmentDto(order.ShipmentStatus, order.ShipmentReference, order.ShipmentVersion, shipmentHistory);
        var paymentDto = new PaymentSimulationDto(payment?.Status ?? "NotStarted", payment?.Version ?? 0, payment?.AttemptId,
            payment?.Amount ?? DemoOrderProjection.Map(order, payment).GrossTotal, payment?.Currency ?? order.Currency, paymentHistory, payment?.RefundedAmount ?? 0);
        await tx.CommitAsync(ct);
        return new(DemoOrderProjection.Map(order, payment), ReservationPlanProjection.Map(plan), order.CancellationReason, order.CancelledBy, order.CancellationRequestedAt, order.CancelledAt, new(order.ProcessingStatus, order.ProcessingVersion, history), paymentDto, shipment, orderReturn);
    }
}
