using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurubDirect.BuildingBlocks;
using SurubDirect.Catalog.Application;
using SurubDirect.Catalog.Contracts;
using SurubDirect.Catalog.Infrastructure;
using Xunit;

namespace SurubDirect.Tests.Integration;
public class TaxonomyTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task Inheritance_filters_rename_and_assignment_changes_preserve_variant_data()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;
        var token = Guid.NewGuid().ToString("N");
        var createAttribute = s.GetRequiredService<CreateAttributeHandler>();
        var pitch = await createAttribute.Handle(new("pitch-" + token, "Pitch", "Number", "mm", 0), default);
        var standard = await createAttribute.Handle(new("standard-" + token, "Standard", "Text", null, 1), default);
        var createCategory = s.GetRequiredService<CreateCategoryHandler>();
        var parent = await createCategory.Handle(new("Parent " + token, "parent-" + token, null, 0, [pitch, standard]), default);
        var child = await createCategory.Handle(new("Child " + token, "child-" + token, parent, 0, []), default);
        var product = await s.GetRequiredService<CreateProductHandler>().Handle(new("Product " + token, "product-" + token, "ignored", "Description", child), default);
        var addVariant = s.GetRequiredService<AddVariantHandler>();
        var a = await addVariant.Handle(product, new("A-" + token, "M8", 8, 40, "Oțel", "Zincat"), default);
        var b = await addVariant.Handle(product, new("B-" + token, "M10", 10, 40, "Oțel", "Zincat"), default);
        var set = s.GetRequiredService<SetVariantAttributesHandler>();
        await set.Handle(product, a, [new(pitch, null, 2), new(standard, "DIN 933", null)], default);
        await set.Handle(product, b, [new(pitch, null, 10), new(standard, "DIN 931", null)], default);
        var reader = s.GetRequiredService<ICatalogReader>();
        Assert.Empty((await reader.List(new(CategoryId: parent, Diameter: 8, Attributes: [new(pitch, Minimum: 9)]), true, default)).Items);
        Assert.Equal(product, Assert.Single((await reader.List(new(CategoryId: parent, Diameter: 8, Attributes: [new(pitch, Minimum: 1, Maximum: 3), new(standard, Text: "din 933")]), true, default)).Items).Id);
        Assert.Equal(product, Assert.Single((await reader.List(new(CategoryId: child, Attributes: [new(pitch, Minimum: 9)]), true, default)).Items).Id);
        await Assert.ThrowsAsync<BusinessException>(() => reader.List(new(CategoryId: child, Attributes: [null!]), true, default));

        var update = s.GetRequiredService<UpdateCategoryHandler>();
        await update.Handle(child, new("Renamed " + token, parent, 0, []), default);
        Assert.Equal("Renamed " + token, (await reader.Get(product, true, default))!.Category);
        await update.Handle(parent, new("Parent " + token, null, 0, []), default);
        Assert.All((await reader.Get(product, true, default))!.Variants, v => Assert.Empty(v.Attributes!));
        await Assert.ThrowsAsync<BusinessException>(() => set.Handle(product, a, [new(pitch, null, 3)], default));
        await update.Handle(parent, new("Parent " + token, null, 0, [pitch, standard]), default);
        Assert.Equal(2, (await reader.Get(product, true, default))!.Variants.Single(x => x.Id == a).Attributes!.Single(x => x.AttributeId == pitch).NumberValue);
        Assert.Equal(4, await s.GetRequiredService<CatalogDbContext>().VariantAttributeValues.CountAsync(x => x.VariantId == a || x.VariantId == b));
        await Assert.ThrowsAsync<BusinessException>(() => set.Handle(product, a, [null!], default));
        Assert.Equal(404, (await Assert.ThrowsAsync<BusinessException>(() => set.Handle(product, Guid.NewGuid(), [], default))).Status);
        await s.GetRequiredService<ArchiveProductHandler>().Handle(product, default);
        Assert.Equal(409, (await Assert.ThrowsAsync<BusinessException>(() => set.Handle(product, a, [], default))).Status);
    }

    [Fact]
    public async Task Concurrent_opposing_moves_cannot_commit_a_cycle()
    {
        Guid a, b;
        var token = Guid.NewGuid().ToString("N");
        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var create = scope.ServiceProvider.GetRequiredService<CreateCategoryHandler>();
            a = await create.Handle(new("A " + token, "a-" + token, null, 0, []), default);
            b = await create.Handle(new("B " + token, "b-" + token, null, 0, []), default);
        }
        async Task<bool> Move(Guid id, Guid parent, string name)
        {
            await using var scope = fixture.Services.CreateAsyncScope();
            try { await scope.ServiceProvider.GetRequiredService<UpdateCategoryHandler>().Handle(id, new(name, parent, 0, []), default); return true; }
            catch (BusinessException) { return false; }
        }
        var results = await Task.WhenAll(Move(a, b, "A " + token), Move(b, a, "B " + token));
        Assert.Single(results, x => x);
        await using var verify = fixture.Services.CreateAsyncScope();
        var categories = await verify.ServiceProvider.GetRequiredService<CatalogDbContext>().Categories.Include(x => x.Attributes).AsNoTracking().ToListAsync();
        SurubDirect.Catalog.Domain.CategoryHierarchy.Validate(categories);
    }
}
