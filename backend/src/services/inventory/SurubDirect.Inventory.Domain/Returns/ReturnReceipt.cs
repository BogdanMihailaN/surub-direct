using SurubDirect.BuildingBlocks;
namespace SurubDirect.Inventory.Domain;
public sealed class ReturnReceipt
{
    private ReturnReceipt() { }
    public Guid Id { get; private set; }
    public Guid ReservationId { get; private set; }
    public Guid OrderId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public List<ReturnReceiptLine> Lines { get; private set; } = [];

    public static ReturnReceipt Create(Guid id, Guid reservationId, Guid orderId, IEnumerable<ReturnReceiptLine>? lines, DateTimeOffset now)
    {
        var items = lines?.ToArray() ?? [];
        if (id == Guid.Empty || reservationId == Guid.Empty || orderId == Guid.Empty
            || items.Length is < 1 or > 100 || items.Any(x => x is null || x.VariantId == Guid.Empty || x.Quantity <= 0)
            || items.Select(x => x.VariantId).Distinct().Count() != items.Length)
            throw new BusinessException("Recepția necesită identificatori valizi și 1–100 SKU-uri distincte cu cantități pozitive.");
        return new() { Id = id, ReservationId = reservationId, OrderId = orderId, CreatedAt = new DateTimeOffset(now.UtcTicks - now.UtcTicks % 10, TimeSpan.Zero),
            Lines = items.OrderBy(x => x.VariantId).ToList() };
    }

    public void EnsureSame(ReturnReceipt other)
    {
        if (ReservationId != other.ReservationId || OrderId != other.OrderId || !Lines.OrderBy(x => x.VariantId).SequenceEqual(other.Lines))
            throw new BusinessException("Identificatorul recepției a fost utilizat pentru alte cantități sau altă comandă.", 409);
    }
}
