namespace SurubDirect.Store.Application.Imports;
public sealed record ImportPreview(IReadOnlyList<ImportRow> Rows, IReadOnlyList<ImportError> Errors,
    int ProductCount, int VariantCount, int OfferCount)
{
    public bool CanImport => Errors.Count == 0 && OfferCount > 0;
}
