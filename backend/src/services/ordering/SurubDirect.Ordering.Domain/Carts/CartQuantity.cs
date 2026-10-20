namespace SurubDirect.Ordering.Domain;

public static class CartQuantity
{
    public static bool IsValid(int packs, int units, int minimum, int step) => units > 0 && minimum > 0 && step > 0 && packs >= minimum && (packs - minimum) % step == 0 && (long)packs * units <= int.MaxValue;
}
