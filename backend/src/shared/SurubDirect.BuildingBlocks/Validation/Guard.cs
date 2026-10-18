namespace SurubDirect.BuildingBlocks;
public static class Guard
{
    public static string Text(string? value, string field, int max = 200)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > max)
            throw new BusinessException($"{field}: valoare obligatorie, maximum {max} caractere.");
        return value.Trim();
    }

    public static void Positive(int value, string field)
    {
        if (value <= 0)
            throw new BusinessException($"{field} trebuie să fie pozitiv.");
    }
}
