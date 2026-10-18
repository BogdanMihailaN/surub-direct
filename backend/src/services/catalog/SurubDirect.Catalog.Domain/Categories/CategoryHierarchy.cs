using SurubDirect.BuildingBlocks;

namespace SurubDirect.Catalog.Domain;
public static class CategoryHierarchy
{
    public static void Validate(IEnumerable<Category> categories)
    {
        var nodes = categories.ToDictionary(x => x.Id);
        foreach (var category in nodes.Values)
        {
            var seen = new HashSet<Guid> { category.Id };
            var parent = category.ParentId;
            while (parent is { } id)
            {
                if (!seen.Add(id)) throw new BusinessException("Categoria nu poate forma un ciclu.");
                if (!nodes.TryGetValue(id, out var node)) throw new BusinessException("Categoria părinte nu există.", 404);
                if (seen.Count > 5) throw new BusinessException("Sunt permise maximum 5 niveluri de categorii.");
                parent = node.ParentId;
            }
        }
    }
}
