namespace SurubDirect.Purchasing.Contracts;

public sealed record PurchaseReceiptCorrectionDto(Guid Id, Guid ReceiptId, Guid LineId, int Quantity, string Reference, string Reason, string Actor, DateTimeOffset CreatedAt, DateTimeOffset? AppliedAt);
