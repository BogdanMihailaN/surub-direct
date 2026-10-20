using SurubDirect.BuildingBlocks;
namespace SurubDirect.Ordering.Domain;

public sealed record CartSelection(string ProductSlug, Guid OfferId, int Packs)
{
    public void Validate() { if (string.IsNullOrWhiteSpace(ProductSlug) || ProductSlug.Length > 200 || ProductSlug.Any(c => !(char.IsAsciiLetterOrDigit(c) || c == '-')) || OfferId == Guid.Empty || Packs < 1) throw new BusinessException("Articol sau cantitate invalidă în coș."); }
}
