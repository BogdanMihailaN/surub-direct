namespace SurubDirect.Catalog.Application;
public record UpdateProduct(string Name, string Category, string Description, Guid? CategoryId = null);
