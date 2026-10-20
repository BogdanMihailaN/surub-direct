using SurubDirect.BuildingBlocks;
using SurubDirect.Inventory.Domain;
using Xunit;
namespace SurubDirect.Tests;
public sealed class StockAdjustmentDomainTests
{
    [Theory]
    [InlineData(0)] [InlineData(-10000001)] [InlineData(10000001)]
    public void Invalid_deltas_are_rejected(int quantity) => Assert.Throws<BusinessException>(()=>StockAdjustment.Create(Guid.NewGuid(),Guid.NewGuid(),quantity,0,0,"DOC","Motiv","admin"));
    [Fact]
    public void Reason_is_required_and_overflow_is_rejected()
    {
        Assert.Throws<BusinessException>(()=>StockAdjustment.Create(Guid.NewGuid(),Guid.NewGuid(),1,0,0,"DOC"," ","admin"));
        var a=StockAdjustment.Create(Guid.NewGuid(),Guid.NewGuid(),1,long.MaxValue,0,"DOC","Motiv","admin");
        Assert.Throws<BusinessException>(()=>a.Apply(long.MaxValue,0));
    }
}
