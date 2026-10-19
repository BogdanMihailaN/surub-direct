using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Purchasing.Application;
using SurubDirect.Purchasing.Contracts;
using SurubDirect.Purchasing.Infrastructure;
using Xunit;
namespace SurubDirect.Tests;
public sealed class SupplierPersistenceTests(PurchasingFixture fixture) : IClassFixture<PurchasingFixture>
{
    private static Task<Guid> Create(IServiceProvider s) => s.GetRequiredService<CreateSupplierHandler>().Handle(new("Furnizor test", "RO", Guid.NewGuid().ToString("N"), "", "", "", "", ""), default);
    private static UpdateSupplier Details(SupplierDto s, bool active) => new(s.Name, s.CountryCode, s.TaxId, s.ContactName, s.Email, s.Phone, s.Address, s.Notes, active);
    [Fact]
    public async Task Supplier_items_can_be_edited_inactivated_and_found_by_sku()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider;
        var id = await Create(s); var variant = Guid.NewGuid();
        var itemId = await s.GetRequiredService<AddSupplierItemHandler>().Handle(id, new(variant, "CODE", "Cutie", 100, 25, "EUR"), default);
        await s.GetRequiredService<UpdateSupplierItemHandler>().Handle(id, itemId, new("CODE2", "Pungă", 50, 12.5m, "RON", false), default);
        var supplier = await s.GetRequiredService<GetSupplierHandler>().Handle(id, default);
        var item = Assert.Single(supplier.Items); Assert.Equal(itemId, item.Id); Assert.Equal(variant, item.VariantId);
        Assert.Equal(50, item.UnitsPerPack); Assert.Equal(12.5m, item.NetCostPerPack); Assert.False(item.IsActive);
        var results = await s.GetRequiredService<ListSuppliersHandler>().Handle(item.Sku, null, 1, default);
        Assert.Equal(id, Assert.Single(results.Items).Id);
        await s.GetRequiredService<UpdateSupplierHandler>().Handle(id, Details(supplier, false), default);
        await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<AddSupplierItemHandler>().Handle(id, new(Guid.NewGuid(), "NEXT", "Cutie", 1, 1, "RON"), default));
        supplier = await s.GetRequiredService<GetSupplierHandler>().Handle(id, default); Assert.False(supplier.IsActive); Assert.Single(supplier.Items);
        await s.GetRequiredService<UpdateSupplierHandler>().Handle(id, Details(supplier, true), default);
        await s.GetRequiredService<UpdateSupplierItemHandler>().Handle(id, itemId, new("CODE2", "Pungă", 50, 12.5m, "RON", true), default);
        Assert.True(Assert.Single((await s.GetRequiredService<GetSupplierHandler>().Handle(id, default)).Items).IsActive);
    }
    [Fact]
    public async Task Fiscal_identity_is_unique_and_one_sku_can_have_multiple_suppliers()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider;
        var first = await Create(s); var second = await Create(s); var variant = Guid.NewGuid();
        foreach (var id in new[] { first, second }) await s.GetRequiredService<AddSupplierItemHandler>().Handle(id, new(variant, "SAME-CODE", "Cutie", 10, 5, "RON"), default);
        var supplier = await s.GetRequiredService<GetSupplierHandler>().Handle(first, default);
        await Assert.ThrowsAsync<DbUpdateException>(() => s.GetRequiredService<CreateSupplierHandler>().Handle(new("Duplicate", "RO", "RO" + supplier.TaxId, "", "", "", "", ""), default));
    }
    [Fact]
    public async Task Concurrent_duplicate_links_are_serialized_without_partial_writes()
    {
        Guid id; await using (var initial = fixture.Services.CreateAsyncScope()) id = await Create(initial.ServiceProvider);
        var variant = Guid.NewGuid();
        async Task<bool> Add()
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<AddSupplierItemHandler>().Handle(id, new(variant, "CODE", "Cutie", 1, 1, "RON"), default); return true; }
            catch (BusinessException e) { Assert.Equal(409, e.Status); return false; }
        }
        Assert.Single(await Task.WhenAll(Add(), Add()), x => x);
        await using var check = fixture.Services.CreateAsyncScope();
        Assert.Single((await check.ServiceProvider.GetRequiredService<GetSupplierHandler>().Handle(id, default)).Items);
    }
    [Fact]
    public async Task Unknown_variants_and_items_of_other_suppliers_do_not_mutate_data()
    {
        await using var scope = fixture.Services.CreateAsyncScope(); var s = scope.ServiceProvider; var first = await Create(s); var second = await Create(s);
        await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<AddSupplierItemHandler>().Handle(first, new(Guid.Empty,"CODE","Cutie",1,1,"RON"), default));
        var item = await s.GetRequiredService<AddSupplierItemHandler>().Handle(first, new(Guid.NewGuid(),"CODE","Cutie",1,1,"RON"), default);
        var error = await Assert.ThrowsAsync<BusinessException>(() => s.GetRequiredService<UpdateSupplierItemHandler>().Handle(second,item,new("BAD","Cutie",1,1,"RON",true),default));
        Assert.Equal(404,error.Status);
        Assert.Empty((await s.GetRequiredService<GetSupplierHandler>().Handle(second, default)).Items);
    }
}
