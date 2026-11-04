using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Data.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The database's own guards (VCST-5918): one list per (user, store, organization), and one row per product and
/// configuration in a list. The X-API looks a list or an item up before it adds one, so these indexes only matter
/// when two requests race; they turn the second into an error instead of a duplicate.
/// </summary>
/// <remarks>
/// SQLite, like PostgreSQL and MySQL, treats NULLs in a unique index as distinct, and on SQL Server EF filters the
/// index to <c>[OrganizationId] IS NOT NULL</c>; so what these tests show for a list without an organization is
/// what every supported provider does.
/// </remarks>
[Trait("Category", "Component")]
public class CompareListUniqueIndexComponentTests
{
    [Fact]
    public async Task SecondListForTheSameUserStoreAndOrganization_IsRefused()
    {
        using var ctx = ProductComparisonTestContext.Create();
        await ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-1"));

        var act = () => ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-1"));

        await act.Should().ThrowAsync<DbUpdateException>();
        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task ListsDifferingInOneKeyPart_AreAllowed()
    {
        // The key is all three: the same buyer has a list per store and per organization they buy for.
        using var ctx = ProductComparisonTestContext.Create();

        await ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-1"));
        await ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-2"));
        await ctx.SaveAsync(AccountList("buyer-1", organizationId: "org-1", storeId: ProductComparisonTestContext.OtherStoreId));
        await ctx.SaveAsync(AccountList("buyer-2", organizationId: "org-1"));

        await using var db = ctx.NewDbContext();
        (await db.Set<CompareListEntity>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(4);
    }

    [Fact]
    public async Task SameProductAndConfigurationTwiceInOneList_IsRefused()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var list = await ctx.SaveAsync(AccountList("buyer-1", items: [Item("printer-1", "printers", 0, configurationKey: "key-a")]));

        var changed = await ctx.CompareListService.GetByIdAsync(list.Id);
        changed.Items.Add(Item("printer-1", "laptops", 0, configurationKey: "key-a"));
        var act = () => ctx.SaveAsync(changed);

        // The category doesn't take part in the key: a product with one configuration is in a list once.
        await act.Should().ThrowAsync<DbUpdateException>();
        (await ctx.ReloadAsync(list.Id)).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task SameProductWithAnotherConfigurationOrInAnotherList_IsAllowed()
    {
        using var ctx = ProductComparisonTestContext.Create();

        var list = await ctx.SaveAsync(AccountList("buyer-1", items:
        [
            Item("printer-1", "printers", 0, configurationKey: ""),
            Item("printer-1", "printers", 1, configurationKey: "key-a"),
            Item("printer-1", "printers", 2, configurationKey: "key-b"),
        ]));
        await ctx.SaveAsync(AccountList("buyer-2", items: [Item("printer-1", "printers", 0, configurationKey: "")]));

        (await ctx.ReloadAsync(list.Id)).Items.Select(x => x.ConfigurationKey).Should().Equal("", "key-a", "key-b");
    }

    [Fact]
    public async Task TwoListsWithoutAnOrganization_ForTheSameUserAndStore_AreNotPrevented()
    {
        // The accepted risk of VCST-5918 (PLAN §11.7): NULL never collides in a unique index, so two first saves
        // racing for a guest, or for a buyer without an organization, leave two lists. Nothing stops it here; what
        // keeps it harmless is that every lookup takes the oldest of them, so the second is never read again (a
        // guest's expires, a buyer's stays unused).
        using var ctx = ProductComparisonTestContext.Create();
        var first = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-1", "printers", 0)]));
        var second = await ctx.SaveAsync(GuestList("guest-1", items: [Item("printer-2", "printers", 0)]));
        await ctx.SetDatesAsync(first.Id, createdDate: DateTime.UtcNow.AddMinutes(-1));

        await using (var db = ctx.NewDbContext())
        {
            (await db.Set<CompareListEntity>().Select(x => x.Id).ToListAsync(TestContext.Current.CancellationToken)).Should().BeEquivalentTo([first.Id, second.Id]);
        }

        // The X-API's lookup: one list for the key, without an organization.
        var criteria = new CompareListSearchCriteria
        {
            StoreId = ProductComparisonTestContext.StoreId,
            UserId = "guest-1",
            OrganizationIdIsEmpty = true,
            Take = 1,
        };
        var found = await ctx.CompareListSearchService.SearchAsync(criteria);

        found.TotalCount.Should().Be(2);
        found.Results.Should().ContainSingle().Which.Id.Should().Be(first.Id);
    }
}
