using SurubDirect.Inventory.Domain;

namespace SurubDirect.Inventory.Application;
public sealed record ReserveStock(Guid Id, Guid OrderId, int DurationSeconds, IReadOnlyList<ReservationLine>? Lines);
