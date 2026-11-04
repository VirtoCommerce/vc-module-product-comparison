using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
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
/// never an argument.
/// </summary>
[Trait("Category", "Component")]
public class CompareListAccessGraphQlTests
{
    private const string GuestId = "4d7e2b10-guest-0002";

    [Fact]
    public async Task Guest_UsingARegisteredUsersId_IsRefused()
    {
        // Otherwise anyone who learned a buyer's id could read and edit that buyer's list without signing in.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));

        var query = await ctx.ExecuteAsGuestAsync(Query("buyer-1"));
        var save = await ctx.ExecuteAsGuestAsync(SaveProducts("buyer-1", "printer-2@printers"));
        var clear = await ctx.ExecuteAsGuestAsync(Mutation("clearCompareList", "buyer-1"));

        query.ErrorCode.Should().Be("Unauthorized");
        save.ErrorCode.Should().Be("Unauthorized");
        clear.ErrorCode.Should().Be("Unauthorized");
        query.Root.GetProperty("data").GetProperty("compareList").ValueKind.Should().Be(JsonValueKind.Null, "nothing of the list may leak with the error");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
    }

    [Fact]
    public async Task SignedInBuyer_CannotReadOrChangeAnotherBuyersList()
    {
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        ctx.Users.Add("buyer-2");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));

        var query = await ctx.ExecuteAsBuyerAsync("buyer-2", Query("buyer-1"));
        var save = await ctx.ExecuteAsBuyerAsync("buyer-2", SaveProducts("buyer-1", "printer-2@printers"));
        var clear = await ctx.ExecuteAsBuyerAsync("buyer-2", Mutation("clearCompareList", "buyer-1"));

        query.ErrorCode.Should().Be("Forbidden");
        save.ErrorCode.Should().Be("Forbidden");
        clear.ErrorCode.Should().Be("Forbidden");

        // Nor can a signed-in buyer use a guest's id: a buyer's list is always under their own.
        (await ctx.ExecuteAsBuyerAsync("buyer-2", SaveProducts(GuestId, "printer-2@printers"))).ErrorCode.Should().Be("Forbidden");

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("buyer-1");
    }

    [Fact]
    public async Task SignedInBuyer_UsesTheirOwnList_UnderTheirUserId()
    {
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");

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
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");

        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("BUYER-1", "printer-1@printers"));

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("Buyer-1"))).Data("compareList")).Should().Equal("printer-1@printers#0");
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken)).Should().Equal("buyer-1");
    }

    [Fact]
    public async Task Administrator_CanReadAndChangeABuyersList()
    {
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
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
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"));
        ctx.Users.LockOut("buyer-1");

        (await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).ErrorCode.Should().Be("UserLocked");
        (await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-2@printers"))).ErrorCode.Should().Be("UserLocked");
        (await ctx.ExecuteAsBuyerAsync("buyer-1", Mutation("clearCompareList", "buyer-1"))).ErrorCode.Should().Be("UserLocked");
    }

    [Fact]
    public async Task Organization_ComesFromTheToken_EachOrganizationHasItsOwnList()
    {
        // A buyer of two organizations compares for each separately, with that organization's prices; which one is
        // decided at sign-in and carried by the token, so no argument can point a call at the other.
        using var ctx = ProductComparisonTestContext.Create();
        ctx.Users.Add("buyer-1");

        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "printer-1@printers"), organizationId: "org-1");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "laptop-1@laptops"), organizationId: "org-2");
        await ctx.ExecuteAsBuyerAsync("buyer-1", SaveProducts("buyer-1", "mouse-1"));

        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"), organizationId: "org-1")).Data("compareList")).Should().Equal("printer-1@printers#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"), organizationId: "org-2")).Data("compareList")).Should().Equal("laptop-1@laptops#0");
        Items((await ctx.ExecuteAsBuyerAsync("buyer-1", Query("buyer-1"))).Data("compareList")).Should().Equal("mouse-1@#0");

        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().Select(x => x.OrganizationId).ToListAsync(TestContext.Current.CancellationToken)).Should().BeEquivalentTo(["org-1", "org-2", null]);
    }
}
