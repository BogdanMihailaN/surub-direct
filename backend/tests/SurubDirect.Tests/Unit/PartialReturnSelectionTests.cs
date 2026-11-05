using SurubDirect.BuildingBlocks;
using SurubDirect.Ordering.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class PartialReturnSelectionTests
{
    [Fact]
    public void Snapshot_uses_original_offer_metadata_and_keeps_same_sku_offers_distinct()
    {
        var (order, _) = ReturnSamples.Create();
        var original = order.Lines[0];
        var second = original with { OfferId = Guid.NewGuid(), UnitsPerPack = 5, OfferLabel = "Pungă", Packs = 3 };
        order.Lines.Add(second);
        var request = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(original.OfferId, 1), new(second.OfferId, 2)], "  Retur produse  ", "admin", DateTimeOffset.UtcNow);
        Assert.Equal("Retur produse", request.Reason);
        Assert.Equal(2, request.Lines.Count);
        Assert.Equal(20, request.Lines.Sum(x => x.Quantity));
        Assert.Equal("Pungă", request.Lines.Single(x => x.OfferId == second.OfferId).OfferLabel);
        Assert.All(request.Lines, x => Assert.Equal(original.VariantId, x.VariantId));
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    public void Invalid_pack_counts_are_rejected(int packs)
    {
        var (order, _) = ReturnSamples.Create();
        Assert.Throws<BusinessException>(() => PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, packs)], "Retur produse", "admin", DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Null_duplicate_unknown_and_empty_selections_are_rejected()
    {
        var (order, _) = ReturnSamples.Create();
        var line = new PartialReturnSelection(order.Lines[0].OfferId, 1);
        foreach (var selection in new IEnumerable<PartialReturnSelection>?[] { null, [], [null!], [line, line], [new(Guid.NewGuid(), 1)] })
            Assert.Throws<BusinessException>(() => PartialReturnRequest.Create(Guid.NewGuid(), order, selection, "Retur produse", "admin", DateTimeOffset.UtcNow));
    }
    [Fact]
    public void Decisions_validate_target_reason_and_version()
    {
        var (order, _) = ReturnSamples.Create();
        var r = PartialReturnRequest.Create(Guid.NewGuid(), order, [new(order.Lines[0].OfferId, 1)], "Retur produse", "admin", DateTimeOffset.UtcNow);
        Assert.Throws<BusinessException>(() => r.Decide(Guid.NewGuid(), 0, "Restoring", "Motiv valid", "admin", DateTimeOffset.UtcNow));
        Assert.Throws<BusinessException>(() => r.Decide(Guid.NewGuid(), 0, "Approved", "x", "admin", DateTimeOffset.UtcNow));
        Assert.Throws<BusinessException>(() => r.Decide(Guid.NewGuid(), 1, "Approved", "Motiv valid", "admin", DateTimeOffset.UtcNow));
        Assert.Equal("Requested", r.Status);
    }
}
