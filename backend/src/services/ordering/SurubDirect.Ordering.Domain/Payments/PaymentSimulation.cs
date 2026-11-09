using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Domain;
public sealed class PaymentSimulation
{
    private PaymentSimulation() { }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "";
    public string Status { get; private set; } = "NotStarted";
    public long Version { get; private set; }
    public decimal PartialRefundTotal { get; private set; }
    public decimal RefundedAmount => Status == "Refunded" ? Amount : PartialRefundTotal;
    public Guid? AttemptId { get; private set; }

    public static PaymentSimulation Create(DemoOrder order) => new()
    {
        OrderId = order.Id,
        Amount = order.Lines.Sum(x => x.GrossTotal) + (order.Delivery?.GrossAmount ?? 0),
        Currency = order.Currency
    };

    public PaymentSimulationEvent Apply(string orderStatus, Guid operationId, long expectedVersion, string action, string reason, string actor, DateTimeOffset now)
    {
        reason = reason.Trim();
        ValidateOperation(orderStatus, operationId, expectedVersion, reason, actor);
        var next = (Status, action) switch
        {
            ("NotStarted" or "Failed", "Start") => "Pending",
            ("Pending", "Succeed") => "Paid",
            ("Pending", "Fail") => "Failed",
            ("Paid" or "PartiallyRefunded", "Refund") => "Refunded",
            _ => throw new BusinessException("Tranziția de plată simulată nu este permisă.", 409)
        };
        if (Amount <= 0 || decimal.Round(Amount, 2) != Amount || string.IsNullOrWhiteSpace(Currency))
            throw new BusinessException("Totalul comenzii nu permite simularea plății.", 409);
        if (action == "Start") AttemptId = operationId;
        var entry = new PaymentSimulationEvent(OrderId, operationId, AttemptId!.Value, ++Version, action, Status, next, reason, actor, now)
        { RefundAmount = action == "Refund" ? Amount - RefundedAmount : null };
        Status = next;
        return entry;
    }

    public PaymentSimulationEvent RefundReturn(DemoOrder order, PartialReturnRequest request, Guid operationId,
        long expectedVersion, string reason, string actor, DateTimeOffset now)
    {
        ValidateOperation(order.Status, operationId, expectedVersion, reason, actor);
        if (order.Id != OrderId || request.OrderId != OrderId || request.Status != "Restored" || request.InventoryReceivedAt is null
            || Status is not ("Paid" or "PartiallyRefunded") || order.Currency != Currency)
            throw new BusinessException("Rambursarea necesită o plată confirmată și o recepție finalizată pentru această comandă.", 409);

        decimal amount = 0;
        foreach (var line in request.Lines)
        {
            var original = order.Lines.Single(x => x.OfferId == line.OfferId);
            amount += original.UnitGross * line.Packs;
        }
        if (amount <= 0 || decimal.Round(amount, 2) != amount || amount > Amount - RefundedAmount)
            throw new BusinessException("Valoarea produselor depășește soldul disponibil sau suma este nevalidă.", 409);

        var from = Status;
        PartialRefundTotal += amount;
        Status = PartialRefundTotal == Amount ? "Refunded" : "PartiallyRefunded";
        return new PaymentSimulationEvent(OrderId, operationId, AttemptId!.Value, ++Version, "ReturnRefund",
            from, Status, reason, actor, now) { PartialReturnId = request.Id, RefundAmount = amount };
    }

    private void ValidateOperation(string orderStatus, Guid operationId, long expectedVersion, string reason, string actor)
    {
        if (operationId == Guid.Empty || expectedVersion < 0 || reason.Length is < 3 or > 500
            || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Operația și motivul de 3–500 caractere sunt obligatorii.");
        if (orderStatus != "Confirmed")
            throw new BusinessException("Simulatorul acceptă numai comenzi confirmate.", 409);
        if (expectedVersion != Version || Version >= 1000)
            throw new BusinessException("Starea simulării s-a modificat sau limita de operații a fost atinsă. Actualizează comanda.", 409);
    }

    public void EnsureCancellationAllowed()
    {
        if (Status is "Pending" or "Paid" or "PartiallyRefunded")
            throw new BusinessException("Finalizează simularea în curs sau simulează rambursarea integrală înainte de anulare.", 409);
    }
}
