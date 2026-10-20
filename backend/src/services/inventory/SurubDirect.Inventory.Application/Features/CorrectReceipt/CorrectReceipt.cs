namespace SurubDirect.Inventory.Application;

public sealed record CorrectReceipt(Guid RequestId, Guid ReceiptId, Guid VariantId, int Quantity, string Reference, string Reason);
