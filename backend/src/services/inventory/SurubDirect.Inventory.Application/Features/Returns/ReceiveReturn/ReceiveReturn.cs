using SurubDirect.Inventory.Contracts;
namespace SurubDirect.Inventory.Application;
public sealed record ReceiveReturn(Guid OrderId, IReadOnlyList<ReturnReceiptLineDto>? Lines);
