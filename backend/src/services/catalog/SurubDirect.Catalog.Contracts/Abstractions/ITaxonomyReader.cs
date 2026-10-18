namespace SurubDirect.Catalog.Contracts;
public interface ITaxonomyReader
{
    Task<TaxonomyDto> Get(CancellationToken ct, bool includeEmpty = false);
}
