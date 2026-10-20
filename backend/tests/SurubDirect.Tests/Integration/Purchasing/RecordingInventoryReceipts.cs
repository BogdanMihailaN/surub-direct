using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Tests;
public sealed class RecordingInventoryReceipts : IInventoryReceipts
{
    public Dictionary<Guid, int> Receipts { get; } = [];
    public bool LoseNextReply { get; set; }
    public Task Receive(Guid requestId, Guid variantId, int quantity, string reference, CancellationToken ct)
    {
        Receipts.TryAdd(requestId, quantity);
        if (LoseNextReply) { LoseNextReply = false; throw new BusinessException("Reply lost after stock commit", 503); }
        return Task.CompletedTask;
    }
}
