using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using VirtoCommerce.ProductComparison.Core.Models;
using VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure;
using Xunit;
using static VirtoCommerce.ProductComparison.Tests.ComponentTests.Infrastructure.CompareListTestData;

namespace VirtoCommerce.ProductComparison.Tests.ComponentTests;

/// <summary>
/// The search criteria (VCST-5918), through the real <c>CompareListSearchService</c> over SQLite. The X-API finds a
/// caller's list by store, user and organization; the cleanup finds guest lists by age and registered lists by
/// owner. Each criterion is shown narrowing one seeded set of lists, against what the others would let through.
/// </summary>
[Trait("Category", "Component")]
public class CompareListSearchServiceComponentTests
{
    private const string OtherStore = ProductComparisonTestContext.OtherStoreId;

    [Fact]
    public async Task StoreUserAndOrganization_EachNarrowTheResult()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);

        (await SearchAsync(ctx, new CompareListSearchCriteria { StoreId = OtherStore }))
            .Should().Equal(lists["buyer-1 other store"]);

        (await SearchAsync(ctx, new CompareListSearchCriteria { StoreId = ProductComparisonTestContext.StoreId, UserId = "buyer-1" }))
            .Should().BeEquivalentTo([lists["buyer-1 no organization"], lists["buyer-1 org-1"], lists["buyer-1 org-2"]]);

        (await SearchAsync(ctx, new CompareListSearchCriteria { OrganizationId = "org-1" }))
            .Should().BeEquivalentTo([lists["buyer-1 org-1"], lists["buyer-2 org-1"]]);
    }

    [Fact]
    public async Task OrganizationIdIsEmpty_FindsOnlyListsWithoutAnOrganization()
    {
        // A guest or a buyer without an organization must never get a list kept for one of a buyer's organizations:
        // without this criterion the lookup for (buyer-1, no organization) would match all three of buyer-1's lists.
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);

        (await SearchAsync(ctx, new CompareListSearchCriteria { StoreId = ProductComparisonTestContext.StoreId, UserId = "buyer-1", OrganizationIdIsEmpty = true }))
            .Should().Equal(lists["buyer-1 no organization"]);

        (await SearchAsync(ctx, new CompareListSearchCriteria { OrganizationIdIsEmpty = true }))
            .Should().BeEquivalentTo([lists["buyer-1 no organization"], lists["buyer-1 other store"], lists["guest old"], lists["guest fresh"]]);
    }

    [Fact]
    public async Task IsAnonymous_SplitsGuestListsFromRegisteredOnes()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);

        (await SearchAsync(ctx, new CompareListSearchCriteria { IsAnonymous = true }))
            .Should().BeEquivalentTo([lists["guest old"], lists["guest fresh"]]);

        (await SearchAsync(ctx, new CompareListSearchCriteria { IsAnonymous = false }))
            .Should().HaveCount(lists.Count - 2).And.NotContain([lists["guest old"], lists["guest fresh"]]);

        // Not set: both kinds.
        (await SearchAsync(ctx, new CompareListSearchCriteria())).Should().HaveCount(lists.Count);
    }

    [Fact]
    public async Task ModifiedBefore_FindsOnlyListsLastChangedBeforeIt()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);
        var cutoff = DateTime.UtcNow.AddHours(-48);
        await ctx.SetDatesAsync(lists["guest old"], modifiedDate: cutoff.AddMinutes(-1));
        await ctx.SetDatesAsync(lists["guest fresh"], modifiedDate: cutoff.AddMinutes(1));
        await ctx.SetDatesAsync(lists["buyer-2 org-1"], modifiedDate: cutoff);

        // Strictly before: a list changed exactly at the cutoff is still within its lifetime.
        (await SearchAsync(ctx, new CompareListSearchCriteria { ModifiedBefore = cutoff }))
            .Should().Equal(lists["guest old"]);

        (await SearchAsync(ctx, new CompareListSearchCriteria { IsAnonymous = true, ModifiedBefore = cutoff.AddMinutes(2) }))
            .Should().BeEquivalentTo([lists["guest old"], lists["guest fresh"]]);
    }

    [Fact]
    public async Task DefaultSort_IsOldestFirst_ThenById()
    {
        // The cleanup pages through lists in this order, and the X-API takes the first list of a key: both need it
        // stable, so a race that left two lists for one key always resolves to the same one.
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);
        var now = DateTime.UtcNow;
        var order = new[] { "guest fresh", "buyer-2 org-1", "buyer-1 org-2", "guest old", "buyer-1 other store", "buyer-1 org-1" };
        for (var i = 0; i < order.Length; i++)
        {
            await ctx.SetDatesAsync(lists[order[i]], createdDate: now.AddHours(-10 + i));
        }

        // Two lists created at the same instant fall back to the id.
        await ctx.SetDatesAsync(lists["buyer-1 no organization"], createdDate: now.AddHours(-10));
        var sameInstant = new[] { lists["guest fresh"], lists["buyer-1 no organization"] }.Order(StringComparer.Ordinal).ToArray();

        (await SearchAsync(ctx, new CompareListSearchCriteria()))
            .Should().Equal(sameInstant.Concat(order.Skip(1).Select(x => lists[x])));
    }

    [Fact]
    public async Task ExplicitSort_OverridesTheDefault()
    {
        using var ctx = ProductComparisonTestContext.Create();
        var lists = await SeedAsync(ctx);
        await ctx.SetDatesAsync(lists["guest old"], modifiedDate: DateTime.UtcNow.AddDays(-5));
        await ctx.SetDatesAsync(lists["guest fresh"], modifiedDate: DateTime.UtcNow.AddDays(-1));

        var ids = await SearchAsync(ctx, new CompareListSearchCriteria { IsAnonymous = true, Sort = "ModifiedDate:desc" });

        ids.Should().Equal(lists["guest fresh"], lists["guest old"]);
    }

    /// <summary>Seven lists: buyer-1 in three organizations of one store and without one in another, buyer-2,
    /// and two guests.</summary>
    private static async Task<Dictionary<string, string>> SeedAsync(ProductComparisonTestContext ctx)
    {
        var lists = new Dictionary<string, CompareList>
        {
            ["buyer-1 no organization"] = AccountList("buyer-1"),
            ["buyer-1 org-1"] = AccountList("buyer-1", organizationId: "org-1"),
            ["buyer-1 org-2"] = AccountList("buyer-1", organizationId: "org-2"),
            ["buyer-1 other store"] = AccountList("buyer-1", storeId: OtherStore),
            ["buyer-2 org-1"] = AccountList("buyer-2", organizationId: "org-1"),
            ["guest old"] = GuestList("guest-1"),
            ["guest fresh"] = GuestList("guest-2"),
        };

        await ctx.SaveAsync(lists.Values.ToList());

        return lists.ToDictionary(x => x.Key, x => x.Value.Id);
    }

    private static async Task<IList<string>> SearchAsync(ProductComparisonTestContext ctx, CompareListSearchCriteria criteria)
    {
        criteria.Take = 100;

        return (await ctx.CompareListSearchService.SearchAsync(criteria)).Results.Select(x => x.Id).ToList();
    }
}
