using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ProductComparison.Core;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using VirtoCommerce.ProductComparison.Xapi;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListDocuments;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The store-level settings through the X-API (VCST-5918, PLAN §4.3): compare on or off for a store (AC 4), guest
/// lists on or off (AC 3), and the per-category limit (D3). Each refusal carries the code the storefront plugin
/// reads to show its message; a store reports each setting's default until a test saves another value on it, as an
/// administrator does.
/// </summary>
[Trait("Category", "Component")]
public class CompareListStoreSettingsGraphQlTests
{
    private const string StoreId = ProductComparisonTestContext.StoreId;
    private const string GuestId = "1e8d4f77-guest-0004";

    [Fact]
    public async Task ComparisonDisabled_RefusesEveryMutation_AndTheQueryReturnsNull()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        var saved = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers"))).Data("saveProductsToCompareList");
        var listId = saved.GetProperty("id").GetString();
        var itemId = ItemId(saved, "printer-1");

        await ctx.SetStoreSettingAsync(StoreId, ModuleConstants.Settings.General.Enabled, false);

        // No list rather than an error: with compare off the storefront shows no compare at all.
        var query = await ctx.ExecuteAsGuestAsync(Query(GuestId));
        query.Data("compareList").ValueKind.Should().Be(JsonValueKind.Null);

        string[] guestMutations =
        [
            SaveProducts(GuestId, "printer-2@printers"),
            Mutation("removeCompareListItems", GuestId, $"""itemIds: ["{itemId}"]"""),
            Mutation("clearCompareList", GuestId),
            Mutation("clearCompareListCategory", GuestId, """categoryId: "printers" """),
            Mutation("moveCompareListItem", GuestId, $"""itemId: "{itemId}" position: 0"""),
        ];
        foreach (var mutation in guestMutations)
        {
            (await ctx.ExecuteAsGuestAsync(mutation)).ErrorCode.Should().Be(CompareListErrors.DisabledCode, "it is refused: {0}", mutation);
        }

        var merge = await ctx.ExecuteAsBuyerAsync("buyer-1", Mutation("mergeCompareList", "buyer-1", $"""secondListId: "{listId}" """));
        merge.ErrorCode.Should().Be(CompareListErrors.DisabledCode);

        // Nothing changed, and the setting is per store: the other store still compares.
        await using (var db = ctx.NewDbContext())
        {
            (await db.Set<CompareListItemEntity>().Select(x => x.ProductId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("printer-1");
        }

        var otherStore = await ctx.ExecuteAsGuestAsync(SaveProductsInStore(ProductComparisonTestContext.OtherStoreId, GuestId, "printer-2@printers"));
        Items(otherStore.Data("saveProductsToCompareList")).Should().Equal("printer-2@printers#0");
    }

    [Fact]
    public async Task GuestListsDisabled_RefuseAGuest_ButNotASignedInBuyer()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        // A guest list saved while guest lists were still allowed.
        await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers"));

        await ctx.SetStoreSettingAsync(StoreId, ModuleConstants.Settings.General.AnonymousListEnabled, false);

        // AC 3: the storefront asks the guest to sign in instead.
        (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-2@printers"))).ErrorCode.Should().Be(CompareListErrors.AnonymousDisabledCode);
        (await ctx.ExecuteAsGuestAsync(SaveProducts("new-guest", "printer-2@printers"))).ErrorCode.Should().Be(CompareListErrors.AnonymousDisabledCode);
        (await ctx.ExecuteAsGuestAsync(Mutation("clearCompareList", GuestId))).ErrorCode.Should().Be(CompareListErrors.AnonymousDisabledCode);
        (await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList").ValueKind.Should().Be(JsonValueKind.Null);

        // A signed-in buyer's list is not a guest list.
        var buyer = await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-2@printers"));
        Items(buyer.Data("saveProductsToCompareList")).Should().Equal("printer-2@printers#0");

        await using var db = ctx.NewDbContext();
        var rows = await db.Set<CompareListEntity>().Include(x => x.Items).ToListAsync(TestContext.Current.CancellationToken);
        rows.Select(x => $"{x.UserId}: {string.Join(' ', x.Items.Select(item => item.ProductId))}").Should().BeEquivalentTo(
            [$"{GuestId}: printer-1", "buyer-1: printer-2"],
            "the guest list kept what it had, and no list was created for the new guest");
    }

    [Fact]
    public async Task UnknownStore_IsRefusedWithStoreNotFound_AndNothingIsStored()
    {
        // Every call reads the store's settings first, so a store id the platform doesn't know stops it before a list
        // is read or created.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();

        var query = await ctx.ExecuteAsGuestAsync(Query(GuestId, "unknown-store"));
        var save = await ctx.ExecuteAsGuestAsync(SaveProductsInStore("unknown-store", GuestId, "printer-1@printers"));

        query.ErrorCode.Should().Be(CompareListErrors.StoreNotFoundCode);
        save.ErrorCode.Should().Be(CompareListErrors.StoreNotFoundCode);
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
    }

    [Fact]
    public async Task CategoryAtItsLimit_RefusesTheWholeCall_OtherCategoriesStillTakeProducts()
    {
        // D3: the limit caps each category group, not the list, as the storefront's own limit did.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.SetStoreSettingAsync(StoreId, ModuleConstants.Settings.General.MaxItems, 2);
        await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "printer-1@printers", "printer-2@printers"));

        var refused = await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "laptop-1@laptops", "printer-3@printers"));

        // The whole call is refused: laptop-1, which had room, isn't saved either.
        refused.ErrorCode.Should().Be(CompareListErrors.LimitReachedCode);
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal("printer-1@printers#0", "printer-2@printers#1");

        var laptop = await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "laptop-1@laptops", "laptop-2@laptops"));
        Items(laptop.Data("saveProductsToCompareList")).Should().BeEquivalentTo("printer-1@printers#0", "printer-2@printers#1", "laptop-1@laptops#0", "laptop-2@laptops#1");
    }
}
