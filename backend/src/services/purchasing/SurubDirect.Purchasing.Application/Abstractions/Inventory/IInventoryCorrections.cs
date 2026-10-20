namespace SurubDirect.Purchasing.Application;

public interface IInventoryCorrections { Task Apply(Guid id, Guid receiptId, Guid variantId, int quantity, string reference, string reason, CancellationToken ct); }
