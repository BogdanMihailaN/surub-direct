namespace SurubDirect.Ordering.Contracts;
public sealed record DemoOrderDto(Guid Id, string Number, string Status, DateTimeOffset CreatedAt, DateTimeOffset? FinalizedAt, string Currency, decimal NetTotal, decimal GrossTotal, CustomerDraftDto Customer, IReadOnlyList<PreparedCartLineDto> Lines);
