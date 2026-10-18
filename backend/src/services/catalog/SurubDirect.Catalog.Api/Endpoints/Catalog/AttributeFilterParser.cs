using System.Text.Json;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Contracts;
namespace SurubDirect.Catalog.Api.Endpoints;
internal static class AttributeFilterParser
{
    public static IReadOnlyList<AttributeFilter>? Parse(string? value)
    {
        if (value is null) return null;
        if (value.Length > 8000) throw new BusinessException("Prea multe filtre.");
        try { return JsonSerializer.Deserialize<AttributeFilter[]>(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? throw new BusinessException("Filtre invalide."); }
        catch (JsonException) { throw new BusinessException("Filtre invalide."); }
    }
}
