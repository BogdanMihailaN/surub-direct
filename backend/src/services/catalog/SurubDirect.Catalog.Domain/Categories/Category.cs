using SurubDirect.BuildingBlocks;
using System.Text.RegularExpressions;

namespace SurubDirect.Catalog.Domain;
public sealed class Category
{
    private Category() { }
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";
    public string Slug { get; private set; } = "";
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public List<CategoryAttribute> Attributes { get; private set; } = [];

    public static Category Create(string name, string slug, Guid? parentId, int sortOrder)
    {
        slug = Guard.Text(slug, "Slug categorie", 160);
        if (!Regex.IsMatch(slug, "^[a-z0-9]+(?:-[a-z0-9]+)*$"))
            throw new BusinessException("Slug-ul categoriei trebuie să conțină litere mici, cifre și cratime.");
        var category = new Category { Slug = slug };
        category.Update(name, parentId, sortOrder);
        return category;
    }
    public void Update(string name, Guid? parentId, int sortOrder)
    {
        var validName = Guard.Text(name, "Categorie", 80);
        if (parentId == Id || sortOrder < 0 || sortOrder > 10000)
            throw new BusinessException("Părinte sau ordine de afișare invalidă.");
        Name = validName;
        NormalizedName = validName.ToUpperInvariant();
        ParentId = parentId;
        SortOrder = sortOrder;
    }
    public void SetAttributes(IEnumerable<Guid> ids)
    {
        var selected = ids.Distinct().ToHashSet();
        Attributes.RemoveAll(x => !selected.Contains(x.AttributeId));
        foreach (var id in selected.Where(id => Attributes.All(x => x.AttributeId != id)))
            Attributes.Add(new CategoryAttribute(Id, id));
    }
}
