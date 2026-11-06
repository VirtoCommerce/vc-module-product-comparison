using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CatalogModule.Core.Model;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// A product deleted from the catalog leaves every compare list (VCST-5918, PLAN §4.5 and §11.5). The products are
/// the catalog module's own, and so is each change: a test deletes, edits or adds through its services, and the
/// catalog announces the change with its <c>ProductChangedEvent</c>, through the platform's in-process event bus, to
/// the module's real handler, which deletes the items set-based and expires the caches its delete bypasses. So a
/// change in how the catalog announces a deletion fails here, as one in the handler does. The class runs alone
/// (<see cref="CompareListCacheCollection"/>), so no other test's cache expiry can stand in for the handler's.
/// </summary>
[Trait("Category", "Component")]
[Collection(CompareListCacheCollection.Name)]
public class ProductChangedEventHandlerComponentTests
{
    [Fact]
    public async Task DeletedProduct_LeavesEveryList()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await AddProductsAsync(ctx);
        var guestList = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0), Item("printer-2", "printers", 1)]));
        var buyerList = await ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-1", storeId: ProductComparisonTestContext.OtherStoreId, items:
        [
            Item("printer-1", "printers", 0, configurationKey: "key-a", sections: [ProductSection("ink", "ink-black"), TextSection("engraving", "Hi")]),
            Item("printer-1", "printers", 1, configurationKey: ""),
            Item("laptop-1", "laptops", 0),
        ]));

        await ctx.Catalog.DeleteProductsAsync("printer-1");

        (await ctx.ReloadAsync(guestList.Id)).Items.Select(x => x.ProductId).Should().Equal("printer-2");
        (await ctx.ReloadAsync(buyerList.Id)).Items.Select(x => x.ProductId).Should().Equal("laptop-1");

        // The set-based delete leaves the configured item's sections to the database's cascade.
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListItemSectionEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task DeletedCategory_TakesItsProductsOutOfEveryList()
    {
        // A category's products are deleted with it, and the catalog announces each of them as deleted: the
        // handler sees no difference from products deleted one by one, and the other category's product stays.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await AddProductsAsync(ctx);
        var list = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0), Item("printer-2", "printers", 1), Item("laptop-1", "laptops", 0)]));

        await ctx.Catalog.DeleteCategoryAsync("printers");

        (await ctx.ReloadAsync(list.Id)).Items.Select(x => x.ProductId).Should().Equal("laptop-1");
    }

    [Fact]
    public async Task EditedOrAddedProduct_ChangesNothing()
    {
        // Only a deletion removes items. An edit (here a new name) keeps them: a product hidden from the store shows
        // as "no longer available", which is the storefront's call, not this handler's. Nor does a product added with
        // the id of one a list still holds (re-created, or imported back) take that item away.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        var printers = await AddProductsAsync(ctx);
        var list = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0), Item("printer-2", "printers", 1), Item("printer-3", "printers", 2)]));

        await ctx.Catalog.RenameProductAsync("printer-1", "Printer 1, renamed");
        await ctx.Catalog.AddProductAsync("printer-3", "Printer 3", printers, 300m);

        (await ctx.ReloadAsync(list.Id)).Items.Select(x => x.ProductId).Should().Equal("printer-1", "printer-2", "printer-3");
    }

    [Fact]
    public async Task AfterADeletion_TheCachesNoLongerServeTheRemovedItems()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await AddProductsAsync(ctx);
        var list = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0), Item("printer-2", "printers", 1)]));
        var criteria = new CompareListSearchCriteria { StoreId = ProductComparisonTestContext.StoreId, UserId = "guest-1", OrganizationIdIsEmpty = true, Take = 1 };

        // Control: the CRUD cache is on, as in production. Two reads hand out the same cached instance, so a
        // write that forgot to expire it would go on being served.
        var first = await ctx.CompareListService.GetNoCloneAsync(list.Id);
        var second = await ctx.CompareListService.GetNoCloneAsync(list.Id);
        second.Should().BeSameAs(first);
        (await ctx.CompareListSearchService.SearchNoCloneAsync(criteria)).Results.Should().ContainSingle().Which.Should().BeSameAs(first);

        await ctx.Catalog.DeleteProductsAsync("printer-1");

        // Read through the caches again, without expiring anything: the CRUD read, the search the X-API runs, and
        // the X-API's compareList itself.
        (await ctx.CompareListService.GetNoCloneAsync(list.Id)).Items.Select(x => x.ProductId).Should().Equal("printer-2");
        (await ctx.CompareListSearchService.SearchNoCloneAsync(criteria)).Results.Single().Items.Select(x => x.ProductId).Should().Equal("printer-2");

        var response = await ctx.ExecuteAsGuestAsync($$"""{ compareList(storeId: "{{ProductComparisonTestContext.StoreId}}", userId: "guest-1") { itemsCount items { productId } } }""");
        var compareList = response.Data("compareList");
        compareList.GetProperty("itemsCount").GetInt32().Should().Be(1);
        compareList.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("productId").GetString()).Should().Equal("printer-2");
    }

    /// <summary>
    /// The catalog's products the lists hold: two printers and a laptop, each in its kind's category. Returns the
    /// printers' category, for a product a test adds later.
    /// </summary>
    private static async Task<Category> AddProductsAsync(ProductComparisonTestContext ctx)
    {
        await ctx.Catalog.InitializeAsync();
        var printers = await ctx.Catalog.AddCategoryAsync("printers", "Printers");
        var laptops = await ctx.Catalog.AddCategoryAsync("laptops", "Laptops");
        await ctx.Catalog.AddProductAsync("printer-1", "Printer 1", printers, 100m);
        await ctx.Catalog.AddProductAsync("printer-2", "Printer 2", printers, 200m);
        await ctx.Catalog.AddProductAsync("laptop-1", "Laptop 1", laptops, 900m);

        return printers;
    }
}
