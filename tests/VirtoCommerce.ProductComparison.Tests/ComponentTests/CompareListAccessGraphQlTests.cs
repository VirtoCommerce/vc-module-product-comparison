using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListDocuments;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// Who may use which compare list through the X-API (VCST-5918, PLAN §5): the guest-cart rule, run by the builders'
/// gate (account state, then <c>CanAccessCompareList</c>) before any handler. A guest may use only an id no
/// registered user has, a signed-in buyer only their own, an administrator any; the organization is the token's,
/// never an argument. The class runs alone (<see cref="CompareListCacheCollection"/>): two tests need a cached lookup
/// to survive until the next call, and another test's save would expire it and let them pass without the rule.
/// </summary>
[Trait("Category", "Component")]
[Collection(CompareListCacheCollection.Name)]
public class CompareListAccessGraphQlTests
{
    private const string GuestId = "4d7e2b10-guest-0002";

    [Fact]
    public async Task Guest_UsingARegisteredUsersId_IsRefused()
    {
        // Otherwise anyone who learned a buyer's id could read and edit that buyer's list without signing in.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        var (list, guestListId) = await SeedListsAsync(ctx);

        using (new AssertionScope())
        {
            foreach (var (operation, document) in EveryOperation("buyer-1", list, guestListId))
            {
                var response = await ctx.ExecuteAsGuestAsync(document);

                response.ErrorCodes.Should().Equal(["Unauthorized"], "a guest's {0} on a buyer's list is refused", operation);
                CarriesData(response, operation).Should().BeFalse("{0} returns nothing of the list with its error", operation);
            }
        }

        await ExpectSeededListsAsync(ctx);
    }

    [Fact]
    public async Task Guest_SpellingABuyersIdInAnotherCase_GetsOnlyAGuestList()
    {
        // Where ids compare case-sensitively (PostgreSQL, and SQLite here), "BUYER-1" is an id no registered user has,
        // so the guest rule lets it through, once the platform's cached copy of the buyer's account is gone: that cache
        // ignores case and would answer for "BUYER-1" with the buyer, refusing the guest. The platform's search cache
        // ignores case as well: a guest lookup that could also match the buyer's list was answered from the buyer's
        // cached lookup, and the guest read and changed the buyer's list. A guest's lookup now reaches only guest lists.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));
        // Caches the buyer's lookup: a buyer without an organization, as every guest is.
        await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"));
        // The buyer signs in again elsewhere, which changes the account: its cached copy goes, the list lookup stays.
        await ctx.RecordSignInAsync("buyer-1");

        var query = await ctx.ExecuteAsGuestAsync(Query("BUYER-1"));
        var save = await ctx.ExecuteAsGuestAsync(SaveProducts("BUYER-1", "printer-2@printers"));

        query.Data("compareList").ValueKind.Should().Be(JsonValueKind.Null, "the guest has no list of their own yet");
        Items(save.Data("saveProductsToCompareList")).Should().Equal("printer-2@printers#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
    }

    [Fact]
    public async Task Buyer_AfterAGuestAskedForTheirIdInAnotherCase_StillGetsTheirList()
    {
        // The same cache entry in the other order: a guest asks for "BUYER-1" first and finds nothing, and the buyer's
        // own lookup was answered with that cached nothing. The buyer's list was hidden, and their next add went into
        // a second list that the oldest-first lookup never shows.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));

        await ctx.ExecuteAsGuestAsync(Query("BUYER-1"));

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
    }

    [Fact]
    public async Task SignedInBuyer_CannotReadOrChangeAnotherBuyersList()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-2");
        var (list, guestListId) = await SeedListsAsync(ctx);

        using (new AssertionScope())
        {
            foreach (var (operation, document) in EveryOperation("buyer-1", list, guestListId))
            {
                var response = await ctx.ExecuteAsBuyerAsync("buyer-2", document);

                response.ErrorCodes.Should().Equal(["Forbidden"], "another buyer's {0} on buyer-1's list is refused", operation);
                CarriesData(response, operation).Should().BeFalse("{0} returns nothing of the list with its error", operation);
            }
        }

        // Nor can a signed-in buyer use a guest's id: a buyer's list is always under their own.
        (await ctx.ExecuteAsBuyerAsync("buyer-2", SaveProducts(GuestId, "printer-3@printers"))).ErrorCode.Should().Be("Forbidden");

        await ExpectSeededListsAsync(ctx);
    }

    [Fact]
    public async Task SignedInBuyer_UsesTheirOwnList_UnderTheirUserId()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");

        var saved = (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"))).Data("saveProductsToCompareList");

        Items(saved).Should().Equal("printer-1@printers#0");
        await using var db = ctx.NewDbContext();
        var row = await db.Set<CompareListEntity>().SingleAsync(TestContext.Current.CancellationToken);
        (row.UserId, row.OrganizationId, row.IsAnonymous).Should().Be(("buyer-1", (string)null, false));
    }

    [Fact]
    public async Task SignedInBuyer_SpellingTheirIdInAnotherCase_GetsTheListUnderTheTokensId()
    {
        // The access rule accepts a buyer's own id in any case, but ids are stored as given: PostgreSQL and SQLite
        // compare them case-sensitively, SQL Server's default collation doesn't, and the cleanup matches them
        // against the user store exactly. A list saved under "BUYER-1" would be missing for "buyer-1" on two
        // providers and deleted as orphaned on all three, so the caller's own id takes the token's spelling.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");

        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("BUYER-1", "printer-1@printers"));

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("Buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("buyer-1");
    }

    [Fact]
    public async Task Administrator_CanReadAndChangeABuyersList()
    {
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        var saved = (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers", "printer-2@printers"))).Data("saveProductsToCompareList");

        var read = (await ctx.ExecuteAsAdministratorAsync(Query("buyer-1"))).Data("compareList");
        var removed = (await ctx.ExecuteAsAdministratorAsync(Mutation("removeCompareListItems", "buyer-1", $"""itemIds: ["{ItemId(saved, "printer-1")}"]"""))).Data("removeCompareListItems");

        Items(read).Should().Equal("printer-1@printers#0", "printer-2@printers#1");
        Items(removed).Should().Equal("printer-2@printers#1");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-2@printers#1");
    }

    [Fact]
    public async Task LockedOutBuyer_IsRefusedByTheAccountStateCheck()
    {
        // Every endpoint runs the X-API's account-state check before authorizing: a token issued before the account
        // was blocked must stop working, for the buyer's own list too.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));
        await ctx.LockOutUserAsync("buyer-1");

        (await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).ErrorCode.Should().Be("UserLocked");
        (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-2@printers"))).ErrorCode.Should().Be("UserLocked");
        (await ctx.ExecuteAsBuyerAsync("buyer-1", Mutation("clearCompareList", "buyer-1"))).ErrorCode.Should().Be("UserLocked");
    }

    [Fact]
    public async Task Organization_ComesFromTheToken_EachOrganizationHasItsOwnList()
    {
        // A buyer of two organizations compares for each separately, with that organization's prices; which one is
        // decided at sign-in and carried by the token, so no argument can point a call at the other.
        await using var ctx = await ProductComparisonTestContext.CreateAsync();
        await ctx.AddUserAsync("buyer-1");

        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"), organizationId: "org-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "laptop-1@laptops"), organizationId: "org-2");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "mouse-1"));

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"), organizationId: "org-1")).Data("compareList")).Should().Equal("printer-1@printers#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"), organizationId: "org-2")).Data("compareList")).Should().Equal("laptop-1@laptops#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("mouse-1@#0");

        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.OrganizationId).ToListAsync(TestContext.Current.CancellationToken)).Should().BeEquivalentTo(["org-1", "org-2", null]);
    }

    /// <summary>
    /// Each of the seven operations, aimed at <paramref name="userId"/>'s list with arguments that would read or change
    /// it: the query, an add, a remove, a move, a category clear, a clear, and a merge of the guest's list into it. The
    /// gate is the builders' common base (<see cref="CompareListEndpointGateTests"/> checks every builder derives from
    /// it), but a builder can override it, and one that left the base out would let its operation through: only
    /// calling every operation shows each one refused.
    /// </summary>
    private static IEnumerable<(string Operation, string Document)> EveryOperation(string userId, JsonElement list, string guestListId) =>
    [
        ("compareList", Query(userId)),
        ("saveProductsToCompareList", SaveProducts(userId, "laptop-2@laptops")),
        ("removeCompareListItems", Mutation("removeCompareListItems", userId, $"""itemIds: ["{ItemId(list, "printer-1")}"]""")),
        ("moveCompareListItem", Mutation("moveCompareListItem", userId, $"""itemId: "{ItemId(list, "printer-2")}" position: 0""")),
        ("clearCompareListCategory", Mutation("clearCompareListCategory", userId, """categoryId: "printers" """)),
        ("clearCompareList", Mutation("clearCompareList", userId)),
        ("mergeCompareList", Mutation("mergeCompareList", userId, $"""secondListId: "{guestListId}" """)),
    ];

    /// <summary>
    /// buyer-1's list, with two printers in one category so that a move and a category clear would change it, and a
    /// guest's list, the one a merge would take over. Returns buyer-1's list as the save returned it, and the guest's id.
    /// </summary>
    private static async Task<(JsonElement List, string GuestListId)> SeedListsAsync(ProductComparisonTestContext ctx)
    {
        await ctx.AddUserAsync("buyer-1");
        var list = (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers", "printer-2@printers"))).Data("saveProductsToCompareList");
        var guestList = (await ctx.ExecuteAsGuestAsync(SaveProducts(GuestId, "laptop-1@laptops"))).Data("saveProductsToCompareList");

        return (list, guestList.GetProperty("id").GetString());
    }

    /// <summary>Both seeded lists as they were, and no other: nothing refused read into them, wrote to them or merged.</summary>
    private static async Task ExpectSeededListsAsync(ProductComparisonTestContext ctx)
    {
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0", "printer-2@printers#1");
        Items((await ctx.ExecuteAsGuestAsync(Query(GuestId))).Data("compareList")).Should().Equal("laptop-1@laptops#0");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().BeEquivalentTo(["buyer-1", GuestId]);
    }

    // Whether a response carries the operation's data: GraphQL nulls a refused field, or the whole data for a
    // non-null one.
    private static bool CarriesData(GraphQlResponse response, string operation)
        => response.Root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty(operation, out var value)
            && value.ValueKind != JsonValueKind.Null;
}
