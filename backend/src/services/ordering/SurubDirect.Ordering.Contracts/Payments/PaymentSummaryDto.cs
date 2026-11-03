namespace SurubDirect.Ordering.Contracts;
public sealed record PaymentSummaryDto(string Status, decimal Amount, string Currency);
