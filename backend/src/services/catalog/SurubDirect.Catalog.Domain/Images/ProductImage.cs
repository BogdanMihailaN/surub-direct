using SurubDirect.BuildingBlocks;
namespace SurubDirect.Catalog.Domain;
public sealed class ProductImage
{
    private ProductImage() { }
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string AltText { get; private set; } = "";
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public static ProductImage Create(Guid id, Guid productId, string altText, Guid? variantId, int width, int height, int order, bool primary)
    {
        if (id == Guid.Empty || width <= 0 || height <= 0) throw new BusinessException("Imagine invalidă.");
        var image = new ProductImage { Id = id, ProductId = productId, Width = width, Height = height, SortOrder = order, IsPrimary = primary };
        image.Update(altText, variantId);
        return image;
    }
    public void Update(string altText, Guid? variantId)
    {
        AltText = Guard.Text(altText, "Text alternativ", 200);
        VariantId = variantId;
    }
    internal void Arrange(int order, bool primary) { SortOrder = order; IsPrimary = primary; }
}
