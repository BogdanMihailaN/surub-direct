namespace SurubDirect.Ordering.Contracts;
public sealed record ShipmentDto(string Status, string? Reference, long Version, IReadOnlyList<ShipmentChangeDto> History);
