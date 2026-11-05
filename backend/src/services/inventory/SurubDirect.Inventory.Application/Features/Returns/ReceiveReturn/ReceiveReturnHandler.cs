using SurubDirect.Inventory.Contracts;
using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Application;
public sealed class ReceiveReturnHandler(IReturnReceipts receipts, TimeProvider clock)
{
    public async Task<ReturnReceiptDto> Handle(Guid reservationId, Guid receiptId, ReceiveReturn command, CancellationToken ct)
    {
        var candidate = ReturnReceipt.Create(receiptId, reservationId, command.OrderId,
            command.Lines?.Select(x => x is null ? null! : new ReturnReceiptLine(x.VariantId, x.Quantity)), clock.GetUtcNow());
        var receipt = await receipts.Receive(candidate, ct);
        return new(receipt.Id, receipt.ReservationId, receipt.OrderId, receipt.CreatedAt,
            receipt.Lines.Select(x => new ReturnReceiptLineDto(x.VariantId, x.Quantity)).ToArray());
    }
}
