using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
namespace SurubDirect.Tests;

public sealed class RecordingInventoryCorrections : IInventoryCorrections
{
    public Dictionary<Guid, int> Corrections { get; } = [];
    public bool LoseNextReply { get; set; }
    public Task Apply(Guid requestId, Guid receiptId, Guid variantId, int quantity, string reference, string reason, CancellationToken ct)
    { Corrections.TryAdd(requestId, quantity); if (LoseNextReply) { LoseNextReply = false; throw new BusinessException("Lost reply", 503); } return Task.CompletedTask; }
}
