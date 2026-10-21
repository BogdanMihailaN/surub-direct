using SurubDirect.Ordering.Domain;

namespace SurubDirect.Tests;
internal static class CustomerDraftSamples
{
    public static CustomerDraft Person() => new("person", "Client Demo", "client@example.test", "+40 700 000 000", null, null, null, new("Strada Exemplu 1", "București", "Sector 1", "010101", "RO"), true, null);
}
