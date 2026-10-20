namespace SurubDirect.Purchasing.Application;
public sealed record ReceivePurchaseOrder(Guid RequestId, int Version, Guid LineId, int Quantity, string Reference);
