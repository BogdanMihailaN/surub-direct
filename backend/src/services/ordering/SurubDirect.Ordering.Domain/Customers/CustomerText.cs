using SurubDirect.BuildingBlocks;

namespace SurubDirect.Ordering.Domain;
internal static class CustomerText
{
    public static string Required(string? value, int max, string field)
    {
        var result = value?.Trim() ?? "";
        if (result.Length == 0 || result.Length > max || result.Any(char.IsControl))
            throw new BusinessException($"Câmp invalid: {field}.");
        return result;
    }

    public static string? Optional(string? value, int max, string field) => string.IsNullOrWhiteSpace(value) ? null : Required(value, max, field);
}
