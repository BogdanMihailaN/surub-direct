namespace SurubDirect.Purchasing.Application;
public interface IInventoryReceipts
{
    Task Receive(Guid requestId, Guid variantId, int quantity, string reference, CancellationToken ct);
}
