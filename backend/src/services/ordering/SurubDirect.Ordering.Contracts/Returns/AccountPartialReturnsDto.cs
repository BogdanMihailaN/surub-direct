namespace SurubDirect.Ordering.Contracts;
public sealed record AccountPartialReturnsDto(bool CanRequest, IReadOnlyList<AccountReturnOfferDto> Offers, IReadOnlyList<AccountPartialReturnDto> Items);
