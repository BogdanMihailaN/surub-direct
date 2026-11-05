using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Domain;
public sealed class PartialReturnRequest
{
    private PartialReturnRequest() { }
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Status { get; private set; } = "Requested";
    public long Version { get; private set; }
    public string Reason { get; private set; } = "";
    public string Actor { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }
    public List<PartialReturnLine> Lines { get; private set; } = [];
    public Guid? ReceiptOperationId { get; private set; }
    public string? ReceiptReason { get; private set; }
    public string? ReceivedBy { get; private set; }
    public DateTimeOffset? ReceiptRequestedAt { get; private set; }
    public DateTimeOffset? InventoryReceivedAt { get; private set; }
    public DateTimeOffset? NextReceiptAttemptAt { get; private set; }
    public int ReceiptAttempts { get; private set; }
    public string? ReceiptError { get; private set; }
    public Guid? DecisionId { get; private set; }
    public string? DecisionReason { get; private set; }
    public string? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }

    public static PartialReturnRequest Create(Guid id, DemoOrder order, IEnumerable<PartialReturnSelection>? selection, string? reason, string actor, DateTimeOffset now)
    {
        var items = selection?.ToArray() ?? [];
        ValidateReason(reason, actor);
        if (id == Guid.Empty || items.Length is < 1 or > 100 || items.Any(x => x is null || x.OfferId == Guid.Empty || x.Packs <= 0)
            || items.Select(x => x.OfferId).Distinct().Count() != items.Length)
            throw new BusinessException("Selectează 1–100 oferte distincte și cantități pozitive de ambalaje.");
        var result = new PartialReturnRequest { Id = id, OrderId = order.Id, Reason = reason!.Trim(), Actor = actor, CreatedAt = Precision(now) };
        foreach (var item in items.OrderBy(x => x.OfferId))
        {
            var original = order.Lines.SingleOrDefault(x => x.OfferId == item.OfferId);
            if (original is null || item.Packs > original.Packs || original.UnitsPerPack <= 0 || (long)item.Packs * original.UnitsPerPack > int.MaxValue)
                throw new BusinessException("Cantitatea sau oferta nu corespunde comenzii originale.", 409);
            result.Lines.Add(new(original.OfferId, original.VariantId, original.ProductName, original.Sku, original.OfferLabel,
                item.Packs, original.UnitsPerPack, item.Packs * original.UnitsPerPack));
        }
        return result;
    }
    public void EnsureSame(PartialReturnRequest other)
    {
        if (OrderId != other.OrderId || Reason != other.Reason || Actor != other.Actor || !Lines.OrderBy(x => x.OfferId).SequenceEqual(other.Lines))
            throw new BusinessException("Identificatorul cererii a fost folosit pentru alt retur.", 409);
    }
    public void Decide(Guid operationId, long expectedVersion, string? target, string? reason, string actor, DateTimeOffset now)
    {
        ValidateReason(reason, actor);
        if (operationId == Guid.Empty || expectedVersion < 0 || target is not ("Approved" or "Rejected"))
            throw new BusinessException("Decizia și identificatorul operației nu sunt valide.");
        if (DecisionId == operationId)
        {
            if (expectedVersion != 0 || (Status == "Rejected" ? "Rejected" : "Approved") != target || DecisionReason != reason!.Trim() || DecidedBy != actor)
                throw new BusinessException("Identificatorul deciziei a fost folosit cu alt conținut.", 409);
            return;
        }
        if (Status != "Requested" || Version != expectedVersion)
            throw new BusinessException("Cererea s-a modificat. Actualizează retururile.", 409);
        Status = target; Version++;
        DecisionId = operationId; DecisionReason = reason!.Trim(); DecidedBy = actor; DecidedAt = Precision(now);
    }
    public void Receive(Guid operationId, long expectedVersion, string? reason, string actor, bool resalableConfirmed, DateTimeOffset now)
    {
        ValidateReason(reason, actor);
        if (operationId == Guid.Empty || !resalableConfirmed)
            throw new BusinessException("Confirmă primirea tuturor cantităților aprobate în stare revandabilă.");
        if (ReceiptOperationId == operationId)
        {
            if (expectedVersion != 1 || ReceiptReason != reason!.Trim() || ReceivedBy != actor)
                throw new BusinessException("Identificatorul recepției a fost folosit cu alt conținut.", 409);
            return;
        }
        if (Status != "Approved" || expectedVersion != Version)
            throw new BusinessException("Recepția necesită cererea aprobată și versiunea curentă.", 409);
        Status = "Restoring"; Version++;
        ReceiptOperationId = operationId; ReceiptReason = reason!.Trim(); ReceivedBy = actor;
        ReceiptRequestedAt = Precision(now); NextReceiptAttemptAt = Precision(now);
    }
    public void ConfirmReceipt(DateTimeOffset inventoryReceivedAt)
    {
        if (Status != "Restoring" || inventoryReceivedAt == default)
            throw new BusinessException("Confirmarea recepției nu este validă.", 409);
        Status = "Restored"; InventoryReceivedAt = Precision(inventoryReceivedAt);
        NextReceiptAttemptAt = null; ReceiptError = null;
        ReceiptAttempts = (int)Math.Min(ReceiptAttempts + 1L, int.MaxValue);
    }
    public void ReceiptFailed(DateTimeOffset now)
    {
        if (Status != "Restoring") return;
        ReceiptAttempts = (int)Math.Min(ReceiptAttempts + 1L, int.MaxValue);
        ReceiptError = "Recepția Inventory nu este confirmată. Reîncercarea păstrează aceeași cheie.";
        NextReceiptAttemptAt = Precision(now.AddSeconds(Math.Min(300, 5 * (long)ReceiptAttempts)));
    }
    private static DateTimeOffset Precision(DateTimeOffset value) => new(value.UtcTicks - value.UtcTicks % 10, TimeSpan.Zero);
    private static void ValidateReason(string? reason, string actor)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 3 or > 500 || string.IsNullOrWhiteSpace(actor) || actor.Length > 200)
            throw new BusinessException("Un motiv de 3–500 caractere și operatorul sunt obligatorii.");
    }
}
