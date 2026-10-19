using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Domain;
public sealed class Product
{
    private Product()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public Guid CategoryId { get; private set; }
    public Category? Classification { get; private set; }
    public string Category { get; private set; } = "";
    public string Description { get; private set; } = "";
    public ProductStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public List<ProductImage> Images { get; private set; } = [];
    public List<Variant> Variants { get; private set; } = [];

    public static Product Create(string name, string slug, string category, string description)
    {
        slug = Guard.Text(slug, "Slug", 160).ToLowerInvariant();
        if (!Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new BusinessException("Slug-ul poate conține litere mici fără diacritice, cifre și cratime.");
        return new()
        {
            Name = Guard.Text(name, "Denumire"),
            Slug = slug,
            Category = Guard.Text(category, "Categorie", 80),
            Description = Guard.Text(description, "Descriere", 4000)
        };
    }

    public void UpdateDetails(string name, string category, string description)
    {
        EnsureEditable();
        var validName = Guard.Text(name, "Denumire");
        var validCategory = Guard.Text(category, "Categorie", 80);
        var validDescription = Guard.Text(description, "Descriere", 4000);
        Name = validName;
        Category = validCategory;
        Description = validDescription;
    }

    public void AssignCategory(Category category)
    {
        EnsureEditable();
        CategoryId = category.Id;
        Category = category.Name;
        Classification = category;
    }

    public void Archive() => Status = ProductStatus.Archived;

    public void Restore()
    {
        if (Status == ProductStatus.Published)
            throw new BusinessException("Doar un produs arhivat poate fi restaurat.", 409);
        Status = ProductStatus.Draft;
    }

    public void EnsureEditable()
    {
        if (Status == ProductStatus.Archived)
            throw new BusinessException("Restaurează produsul înainte de a-l modifica.", 409);
    }

    public Variant AddVariant(string sku, string label, decimal? diameter, decimal? length, string material, string finish)
    {
        EnsureEditable();
        var variant = Variant.Create(Id, sku, label, diameter, length, material, finish);
        if (Variants.Any(v => v.Sku == variant.Sku))
            throw new BusinessException("SKU duplicat.");
        Variants.Add(variant);
        return variant;
    }

    public void UpdateVariant(Guid variantId, string label, decimal? diameter, decimal? length, string material, string finish)
    {
        EnsureEditable();
        var variant = Variants.SingleOrDefault(x => x.Id == variantId)
            ?? throw new BusinessException("Varianta nu există în acest produs.", 404);
        variant.UpdateDetails(label, diameter, length, material, finish);
    }

    public bool UpdateOffer(Guid variantId, Guid offerId, string label, int units, int min, int step)
    {
        EnsureEditable();
        var variant = Variants.SingleOrDefault(x => x.Id == variantId)
            ?? throw new BusinessException("Varianta nu există în acest produs.", 404);
        var offer = variant.Offers.SingleOrDefault(x => x.Id == offerId)
            ?? throw new BusinessException("Ambalajul nu există pentru această variantă.", 404);
        var packSizeChanged = offer.UnitsPerPack != units;
        offer.UpdateDetails(label, units, min, step);
        if (packSizeChanged)
            Status = ProductStatus.Draft;
        return packSizeChanged;
    }

    public ProductImage AddImage(Guid id, string altText, Guid? variantId, int width, int height)
    {
        EnsureEditable();
        EnsureImageVariant(variantId);
        if (Images.Count >= 12) throw new BusinessException("Maximum 12 imagini per produs.");
        var image = ProductImage.Create(id, Id, altText, variantId, width, height, Images.Count, Images.Count == 0);
        Images.Add(image);
        return image;
    }
    public void UpdateImage(Guid imageId, string altText, Guid? variantId)
    {
        EnsureEditable();
        EnsureImageVariant(variantId);
        Image(imageId).Update(altText, variantId);
    }
    public void ArrangeImages(IReadOnlyList<Guid> ids, Guid primaryId)
    {
        EnsureEditable();
        if (ids is null || ids.Count != Images.Count || ids.Distinct().Count() != ids.Count ||
            ids.Any(id => Images.All(x => x.Id != id)) || !ids.Contains(primaryId))
            throw new BusinessException("Ordinea trebuie să includă toate imaginile, o singură dată, și imaginea principală.");
        for (var i = 0; i < ids.Count; i++) Image(ids[i]).Arrange(i, ids[i] == primaryId);
    }
    public void RemoveImage(Guid imageId)
    {
        EnsureEditable();
        Images.Remove(Image(imageId));
        var ordered = Images.OrderBy(x => x.SortOrder).ToArray();
        var primary = ordered.FirstOrDefault(x => x.IsPrimary) ?? ordered.FirstOrDefault();
        for (var i = 0; i < ordered.Length; i++) ordered[i].Arrange(i, ordered[i] == primary);
    }
    private ProductImage Image(Guid id) => Images.SingleOrDefault(x => x.Id == id)
        ?? throw new BusinessException("Imaginea nu există în acest produs.", 404);
    private void EnsureImageVariant(Guid? id)
    {
        if (id.HasValue && Variants.All(x => x.Id != id.Value)) throw new BusinessException("Varianta nu există în acest produs.", 404);
    }

    public void Publish()
    {
        if (Variants.Count == 0 || Variants.Any(v => v.Offers.Count == 0))
            throw new BusinessException("Publicarea cere variante cu oferte de vânzare.");
        if (Status == ProductStatus.Archived)
            throw new BusinessException("Produsul arhivat nu poate fi publicat.");
        Status = ProductStatus.Published;
    }

    public static string Normalize(string value)
    {
        var chars = value.ToLowerInvariant().Replace('×', 'x').Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(c));
        return string.Concat(chars).Normalize(NormalizationForm.FormC);
    }
}
