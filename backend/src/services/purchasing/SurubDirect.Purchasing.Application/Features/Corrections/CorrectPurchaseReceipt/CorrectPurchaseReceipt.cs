namespace SurubDirect.Purchasing.Application;

public sealed record CorrectPurchaseReceipt(Guid RequestId, int Version, Guid ReceiptId, int Quantity, string Reference, string Reason);
