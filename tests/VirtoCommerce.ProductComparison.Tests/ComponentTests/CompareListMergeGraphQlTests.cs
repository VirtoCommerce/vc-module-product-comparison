using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Xapi;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListDocuments;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// <c>mergeCompareList</c> (VCST-5918, D10): on sign-in the storefront hands the buyer's account the list the
/// guest built, by its id. Only a signed-in buyer merges, only a guest list (or their own) is taken, and the guest
/// list is gone afterwards, so it can't be merged twice or reach anyone else. The rule mergeCart got in VCST-6185.
/// </summary>
[Trait("Category", "Component")]
public class CompareListMergeGraphQlTests
{
    private const string GuestId = "7a51c9d2-guest-0003";

    [Fact]
    public async Task SignedInBuyer_TakesTheGuestListOver_AndTheGuestListIsGone()
    {
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        var guestListId = await SaveGuestListAsync(ctx, ProductComparisonTestContext.StoreId, "printer-1@printers", "laptop-1@laptops");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-2@printers"));

        var merged = await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(guestListId));

        Items(merged.Data("mergeCompareList")).Should().BeEquivalentTo("printer-2@printers#0", "printer-1@printers#1", "laptop-1@laptops#0");
        var accountList = (await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList");
        Categories(accountList).Should().Equal("printers: printer-2 printer-1", "laptops: laptop-1");

        // The guest's list is deleted: the guest id now has none, and the account list is the only row left.
        (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList").ValueKind.Should().Be(JsonValueKind.Null);
        await using (var db = ctx.NewDbContext())
        {
            (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("buyer-1");
        }

        // Merging the same id again (a second tab, a retry) finds nothing to take and changes nothing.
        var again = (await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(guestListId))).Data("mergeCompareList");
        Items(again).Should().BeEquivalentTo(Items(accountList));
    }

    [Fact]
    public async Task Merge_Anonymously_IsRefused()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var guestListId = await SaveGuestListAsync(ctx, ProductComparisonTestContext.StoreId, "printer-1@printers");

        // Another guest, or the guest itself: a merge needs a signed-in account to merge into.
        var otherGuest = await ctx.ExecuteAsGuestAsync(Merge(guestListId, userId: "another-guest"));
        var sameGuest = await ctx.ExecuteAsGuestAsync(Merge(guestListId, userId: GuestId));

        otherGuest.ErrorCode.Should().Be("Unauthorized");
        sameGuest.ErrorCode.Should().Be("Unauthorized");
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal("printer-1@printers#0");
    }

    [Fact]
    public async Task Merge_AGuestListOfAnotherStore_ChangesNothing()
    {
        // A guest list belongs to the store it was built in; merging it into another store's list is a no-op that
        // leaves both lists as they were.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        var otherStoreListId = await SaveGuestListAsync(ctx, ProductComparisonTestContext.OtherStoreId, "printer-1@printers");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-2@printers"));

        var merged = (await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(otherStoreListId))).Data("mergeCompareList");

        Items(merged).Should().Equal("printer-2@printers#0");
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId, ProductComparisonTestContext.OtherStoreId))).Data("compareList")).Should().Equal("printer-1@printers#0");
    }

    [Fact]
    public async Task Merge_AnotherBuyersList_IsRefused()
    {
        // The source must be a guest list or the caller's own; a registered buyer's list id is not a way in.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        ctx.Users.Add("buyer-2");
        var buyer2ListId = (await ctx.ExecuteAsBuyerAsync("buyer-2", SaveProducts("buyer-2", "printer-1@printers"))).Data("saveProductsToCompareList").GetProperty("id").GetString();

        var merged = await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(buyer2ListId));

        merged.ErrorCode.Should().Be("Forbidden");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-2", Query("buyer-2"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("buyer-2");
    }

    [Fact]
    public async Task Merge_TheAccountListIntoItself_ChangesNothing()
    {
        // Without its own check, the merge would take the list's items over from itself and then delete the "source":
        // the buyer's whole list.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        var saved = (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers", "laptop-1@laptops"))).Data("saveProductsToCompareList");

        var merged = (await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(saved.GetProperty("id").GetString()))).Data("mergeCompareList");

        Items(merged).Should().Equal("printer-1@printers#0", "laptop-1@laptops#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0", "laptop-1@laptops#0");
    }

    [Fact]
    public async Task Merge_PastTheListCap_IsRefused_AndTheGuestListIsKept()
    {
        // The account list is at the 500-product cap (100 categories of 5) and the guest compared a product in a
        // category of its own: nothing older can make room. The merge is refused with a code the storefront can tell
        // from a failed request, and neither list changes: the guest's products are not deleted unseen.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        var accountProducts = Enumerable.Range(0, ModuleConstants.MaxListItems).Select(i => $"p{i}@category{i / 5}").ToArray();
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", accountProducts));
        var guestListId = await SaveGuestListAsync(ctx, ProductComparisonTestContext.StoreId, "guest-product@new-category");

        var merged = await ctx.ExecuteAsBuyerAsync("buyer-1", Merge(guestListId));

        merged.ErrorCode.Should().Be(CompareListErrors.FullCode);
        ItemsCount((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Be(ModuleConstants.MaxListItems);
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal("guest-product@new-category#0");
    }

    private static string Merge(string secondListId, string userId = "buyer-1")
    {
        return Mutation("mergeCompareList", userId, $"""secondListId: "{secondListId}" """);
    }

    private static async Task<string> SaveGuestListAsync(ProductComparisonTestContext ctx, string storeId, params string[] products)
    {
        var saved = await ctx.ExecuteAsGuestAsync(SaveProductsInStore(storeId, GuestId, products));

        return saved.Data("saveProductsToCompareList").GetProperty("id").GetString();
    }
}
