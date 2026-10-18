namespace SurubDirect.Catalog.Application;
public record CreateProduct(string Name, string Slug, string Category, string Description, Guid? CategoryId = null);
