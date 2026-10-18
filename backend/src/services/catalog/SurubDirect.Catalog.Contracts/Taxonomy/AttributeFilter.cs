namespace SurubDirect.Catalog.Contracts;
public record AttributeFilter(Guid AttributeId, string? Text = null, decimal? Minimum = null, decimal? Maximum = null);
