namespace SurubDirect.Purchasing.Contracts;
public sealed record PurchaseReceiptDto(Guid Id, Guid LineId, Guid VariantId, int Quantity, string Reference, string Actor, DateTimeOffset CreatedAt, DateTimeOffset? AppliedAt);
