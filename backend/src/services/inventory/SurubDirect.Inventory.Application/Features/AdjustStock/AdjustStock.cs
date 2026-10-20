namespace SurubDirect.Inventory.Application;
public sealed record AdjustStock(Guid RequestId, int Quantity, long ExpectedOnHand, long ExpectedReserved, string Reference, string Reason);
