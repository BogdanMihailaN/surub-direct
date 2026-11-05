using SurubDirect.Inventory.Domain;
namespace SurubDirect.Inventory.Application;
public interface IReturnReceipts
{
    Task<ReturnReceipt> Receive(ReturnReceipt candidate, CancellationToken ct);
}
