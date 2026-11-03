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
        if (operationId == Guid.Empty || expectedVersion < 0 || reason.Length is < 3 or > 500
            || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Operația și motivul de 3–500 caractere sunt obligatorii.");
        if (orderStatus != "Confirmed")
            throw new BusinessException("Simulatorul acceptă numai comenzi confirmate.", 409);
        if (expectedVersion != Version || Version >= 1000)
            throw new BusinessException("Starea simulării s-a modificat sau limita de operații a fost atinsă. Actualizează comanda.", 409);
        var next = (Status, action) switch
        {
            ("NotStarted" or "Failed", "Start") => "Pending",
            ("Pending", "Succeed") => "Paid",
            ("Pending", "Fail") => "Failed",
            ("Paid", "Refund") => "Refunded",
            _ => throw new BusinessException("Tranziția de plată simulată nu este permisă.", 409)
        };
        if (Amount <= 0 || decimal.Round(Amount, 2) != Amount || string.IsNullOrWhiteSpace(Currency))
            throw new BusinessException("Totalul comenzii nu permite simularea plății.", 409);
        if (action == "Start") AttemptId = operationId;
        var entry = new PaymentSimulationEvent(OrderId, operationId, AttemptId!.Value, ++Version, action, Status, next, reason, actor, now);
        Status = next;
        return entry;
    }

    public void EnsureCancellationAllowed()
    {
        if (Status is "Pending" or "Paid")
            throw new BusinessException("Finalizează simularea în curs sau simulează rambursarea integrală înainte de anulare.", 409);
    }
}
